namespace Std.DL.Flux;

/// <summary>
///     ONNX 模型导出器 —— 将 Galatea 模型导出为 .onnx 格式
///     支持 GPTModel 和 WeightTiedGPTModel
///     使用 ONNX opset 17，将 Galatea 算子映射为标准 ONNX 算子
///     内置轻量级 protobuf 写入器，不依赖任何外部 NuGet 包
/// </summary>
public sealed class OnnxExporter
{
    /// <summary>
    ///     ONNX opset 版本
    /// </summary>
    private const int OpsetVersion = 17;

    /// <summary>
    ///     ONNX IR 版本
    /// </summary>
    private const long IrVersion = 8;

    /// <summary>
    ///     ONNX 数据类型：FLOAT = 1
    /// </summary>
    private const int OnnxFloat = 1;

    /// <summary>
    ///     ONNX 数据类型：INT64 = 7
    /// </summary>
    private const int OnnxInt64 = 7;

    /// <summary>
    ///     AttributeProto 类型：FLOAT = 1
    /// </summary>
    private const int AttrTypeFloat = 1;

    /// <summary>
    ///     AttributeProto 类型：INT = 2
    /// </summary>
    private const int AttrTypeInt = 2;

    /// <summary>
    ///     AttributeProto 类型：INTS = 7
    /// </summary>
    private const int AttrTypeInts = 7;

    private readonly List<InitializerRecord> _initializers;
    private readonly List<ValueInfoRecord> _inputs;

    private readonly List<NodeRecord> _nodes;
    private readonly List<ValueInfoRecord> _outputs;
    private readonly HashSet<string> _usedNames;
    private string _graphName;
    private int _nameCounter;

    private OnnxExporter()
    {
        _nodes = [];
        _initializers = [];
        _inputs = [];
        _outputs = [];
        _usedNames = [];
        _nameCounter = 0;
        _graphName = "galatea_model";
    }

    #region Transformer 解码器层导出

    /// <summary>
    ///     导出 Transformer 解码器层（Pre-Norm 结构）
    ///     x = x + Attention(RMSNorm(x))
    ///     x = x + FFN(RMSNorm(x))
    /// </summary>
    /// <param name="layer">解码器层</param>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="maskName">因果掩码名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportTransformerDecoderLayer(TransformerDecoderLayer layer, string inputName, string maskName,
        string prefix)
    {
        var normed1 = ExportRMSNorm(layer.Norm1, inputName, $"{prefix}.norm1");

        var attnOut = ExportMultiHeadAttention(layer.Attention, normed1, maskName, $"{prefix}.attn");

        var res1 = AddAddNode(inputName, attnOut, $"{prefix}.residual1");

        var normed2 = ExportRMSNorm(layer.Norm2, res1, $"{prefix}.norm2");

        var ffnOut = ExportSwiGLUFFN((SwiGLUFFN)layer.FFN, normed2, $"{prefix}.ffn");

        var res2 = AddAddNode(res1, ffnOut, $"{prefix}.residual2");

        return res2;
    }

    #endregion

    #region 注意力层导出

    /// <summary>
    ///     导出多头注意力层（分解为 Transpose + MatMul + Softmax + MatMul）
    ///     管线：QKV 投影 → Split → 转置 → 缩放点积注意力 → 输出投影
    /// </summary>
    /// <param name="mha">多头注意力层</param>
    /// <param name="inputName">输入张量名称 [batch, seqLen, dModel]</param>
    /// <param name="maskName">因果掩码名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称 [batch, seqLen, dModel]</returns>
    private string ExportMultiHeadAttention(MultiHeadAttention mha, string inputName, string maskName, string prefix)
    {
        var dModel = mha._dModel;
        var numHeads = mha._numHeads;
        var dK = mha._dK;

        var qkvName = ExportDense(mha._qkvProj, inputName, $"{prefix}.qkv_proj");

        var splitSize = numHeads * dK;
        var (qFlat, kFlat, vFlat) = AddSplitNode(qkvName, 2, [splitSize, splitSize, splitSize],
            $"{prefix}.q", $"{prefix}.k", $"{prefix}.v");

        var q = AddReshapeThenTranspose(qFlat, [-1, -1, numHeads, dK], [0, 2, 1, 3],
            $"{prefix}.q_4d");
        var k = AddReshapeThenTranspose(kFlat, [-1, -1, numHeads, dK], [0, 2, 1, 3],
            $"{prefix}.k_4d");
        var v = AddReshapeThenTranspose(vFlat, [-1, -1, numHeads, dK], [0, 2, 1, 3],
            $"{prefix}.v_4d");

        var kT = AddTransposeNode(k, [0, 1, 3, 2], $"{prefix}.k_t");

        var scores = AddMatMulNode(q, kT, $"{prefix}.scores");

        var scaleValue = 1.0f / MathF.Sqrt(dK);
        var scaleName = AddScalarInitializer($"{prefix}.scale", scaleValue);
        scores = AddMulNode(scores, scaleName, $"{prefix}.scaled_scores");

        scores = AddAddNode(scores, maskName, $"{prefix}.masked_scores");

        var weights = AddSoftmaxNode(scores, -1, $"{prefix}.attn_weights");

        var attnOut = AddMatMulNode(weights, v, $"{prefix}.attn_out");

        attnOut = AddTransposeNode(attnOut, [0, 2, 1, 3], $"{prefix}.attn_out_t");

        attnOut = AddReshapeNode(attnOut, [-1, -1, dModel], $"{prefix}.attn_out_flat");

        return ExportDense(mha._outProj, attnOut, $"{prefix}.out_proj");
    }

    #endregion

    #region 前馈网络导出

    /// <summary>
    ///     导出 SwiGLU 前馈网络
    ///     分解为：MatMul + Mul + Silu + MatMul + Mul + Add
    ///     output = DownProj(SiLU(GateProj(x)) ⊙ UpProj(x))
    /// </summary>
    /// <param name="ffn">SwiGLU FFN 层</param>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportSwiGLUFFN(SwiGLUFFN ffn, string inputName, string prefix)
    {
        var gate = ExportDense(ffn.GateProj, inputName, $"{prefix}.gate_proj");

        var siluGate = ExportSiLU(gate, $"{prefix}.silu_gate");

        var up = ExportDense(ffn.UpProj, inputName, $"{prefix}.up_proj");

        var gated = AddMulNode(siluGate, up, $"{prefix}.gated");

        return ExportDense(ffn.DownProj, gated, $"{prefix}.down_proj");
    }

    #endregion

    #region 公开导出方法

    /// <summary>
    ///     导出 GPTModel 到 ONNX 文件
    /// </summary>
    /// <param name="model">GPT 模型</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="seqLen">序列长度</param>
    public static void Export(GPTModel model, string outputPath, int batchSize = 1, int seqLen = 512)
    {
        var exporter = new OnnxExporter();
        exporter.ExportGPTModel(model, batchSize, seqLen);
        exporter.WriteToFile(outputPath);
    }

    /// <summary>
    ///     导出 WeightTiedGPTModel 到 ONNX 文件
    /// </summary>
    /// <param name="model">权重共享 GPT 模型</param>
    /// <param name="outputPath">输出文件路径</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="seqLen">序列长度</param>
    public static void Export(WeightTiedGPTModel model, string outputPath, int batchSize = 1, int seqLen = 512)
    {
        var exporter = new OnnxExporter();
        exporter.ExportWeightTiedGPTModel(model, batchSize, seqLen);
        exporter.WriteToFile(outputPath);
    }

    /// <summary>
    ///     构建 GPTModel 的 ONNX 字节数组
    /// </summary>
    /// <param name="model">GPT 模型</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="seqLen">序列长度</param>
    /// <returns>ONNX protobuf 字节数组</returns>
    public static byte[] BuildBytes(GPTModel model, int batchSize = 1, int seqLen = 512)
    {
        var exporter = new OnnxExporter();
        exporter.ExportGPTModel(model, batchSize, seqLen);
        return exporter.BuildModelBytes();
    }

    /// <summary>
    ///     构建 WeightTiedGPTModel 的 ONNX 字节数组
    /// </summary>
    /// <param name="model">权重共享 GPT 模型</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="seqLen">序列长度</param>
    /// <returns>ONNX protobuf 字节数组</returns>
    public static byte[] BuildBytes(WeightTiedGPTModel model, int batchSize = 1, int seqLen = 512)
    {
        var exporter = new OnnxExporter();
        exporter.ExportWeightTiedGPTModel(model, batchSize, seqLen);
        return exporter.BuildModelBytes();
    }

    #endregion

    #region GPT 模型导出核心

    /// <summary>
    ///     导出 GPTModel 的完整计算图
    ///     管线：inputIds → TokenEmbedding → [TransformerDecoderLayer × N] → FinalNorm → LmHead → logits
    /// </summary>
    /// <param name="model">GPT 模型</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="seqLen">序列长度</param>
    private void ExportGPTModel(GPTModel model, int batchSize, int seqLen)
    {
        _graphName = "gpt_model";

        var inputName = "input_ids";
        AddInput(inputName, OnnxInt64, batchSize, seqLen);

        var embOut = ExportEmbedding(model.TokenEmbedding, inputName, "token_emb");

        var maskName = CreateCausalMaskInitializer(seqLen, "causal_mask");

        var x = embOut;
        for (var i = 0; i < model.NumLayers; i++)
            x = ExportTransformerDecoderLayer(model.Layers[i], x, maskName, $"layer_{i}");

        x = ExportRMSNorm(model.FinalNorm, x, "final_norm");
        x = ExportDense(model.LmHead, x, "lm_head");

        AddOutput(x, OnnxFloat, batchSize, seqLen, model.VocabSize);
    }

    /// <summary>
    ///     导出 WeightTiedGPTModel 的完整计算图
    ///     LM Head 使用 Embedding.Weight 的转置（权重共享）
    /// </summary>
    /// <param name="model">权重共享 GPT 模型</param>
    /// <param name="batchSize">批次大小</param>
    /// <param name="seqLen">序列长度</param>
    private void ExportWeightTiedGPTModel(WeightTiedGPTModel model, int batchSize, int seqLen)
    {
        _graphName = "weight_tied_gpt_model";

        var inputName = "input_ids";
        AddInput(inputName, OnnxInt64, batchSize, seqLen);

        var embWeightName = AddInitializerFromArray("token_emb_weight", model.TokenEmbedding.Weight);

        var embOut = AddGatherNode(inputName, embWeightName, 0, "token_emb_output");

        var maskName = CreateCausalMaskInitializer(seqLen, "causal_mask");

        var x = embOut;
        for (var i = 0; i < model.NumLayers; i++)
            x = ExportTransformerDecoderLayer(model.Layers[i], x, maskName, $"layer_{i}");

        x = ExportRMSNorm(model.FinalNorm, x, "final_norm");

        var weightTName = AddTransposeNode(embWeightName, [1, 0], "lm_head_weight_t");
        x = AddMatMulNode(x, weightTName, "lm_head_matmul");

        AddOutput(x, OnnxFloat, batchSize, seqLen, model.VocabSize);
    }

    #endregion

    #region 归一化层导出

    /// <summary>
    ///     导出 RMSNorm（分解为 Sqrt + Mul + Add + Mul）
    ///     y = x * gamma / RMS(x)，其中 RMS(x) = sqrt(mean(x^2) + epsilon)
    ///     分解步骤：
    ///     1. x_sq = Mul(x, x)
    ///     2. mean_sq = ReduceMean(x_sq, axis=-1, keepdims=1)
    ///     3. mean_sq_eps = Add(mean_sq, epsilon)
    ///     4. rms = Sqrt(mean_sq_eps)
    ///     5. inv_rms = Recip(rms)
    ///     6. x_norm = Mul(x, inv_rms)
    ///     7. output = Mul(x_norm, gamma)
    /// </summary>
    /// <param name="norm">RMSNorm 层</param>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportRMSNorm(RMSNorm norm, string inputName, string prefix)
    {
        var xSq = AddMulNode(inputName, inputName, $"{prefix}.x_sq");

        var meanSq = AddReduceMeanNode(xSq, [-1], true, $"{prefix}.mean_sq");

        var epsName = AddScalarInitializer($"{prefix}.epsilon", norm._epsilon);
        var meanSqEps = AddAddNode(meanSq, epsName, $"{prefix}.mean_sq_eps");

        var rms = AddSqrtNode(meanSqEps, $"{prefix}.rms");

        var invRms = AddRecipNode(rms, $"{prefix}.inv_rms");

        var xNorm = AddMulNode(inputName, invRms, $"{prefix}.x_norm");

        var gammaName = AddInitializerFromArray($"{prefix}.gamma", norm.Gamma);
        return AddMulNode(xNorm, gammaName, $"{prefix}.output");
    }

    /// <summary>
    ///     导出 LayerNorm（使用 ONNX opset 17+ 原生 LayerNormalization 算子）
    /// </summary>
    /// <param name="norm">LayerNorm 层</param>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportLayerNorm(LayerNorm norm, string inputName, string prefix)
    {
        var outputName = UniqueName($"{prefix}.output");
        var gammaName = AddInitializerFromArray($"{prefix}.gamma", norm.Gamma);
        var betaName = AddInitializerFromArray($"{prefix}.beta", norm.Beta);

        var attrs = new List<AttrRecord>
        {
            new("epsilon", AttrTypeFloat, norm._epsilon),
            new("axis", AttrTypeInt, IntValue: -1)
        };

        _nodes.Add(new NodeRecord(
            "LayerNormalization",
            UniqueName($"{prefix}.layernorm"),
            [inputName, gammaName, betaName],
            [outputName],
            attrs
        ));

        return outputName;
    }

    #endregion

    #region 基础层导出

    /// <summary>
    ///     导出 Dense 层（分解为 MatMul + Add）
    ///     output = input × Weight + Bias
    /// </summary>
    /// <param name="dense">全连接层</param>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportDense(Dense dense, string inputName, string prefix)
    {
        var weightName = AddInitializerFromArray($"{prefix}.weight", dense.Weight);
        var matmulOut = AddMatMulNode(inputName, weightName, $"{prefix}.matmul");

        var biasName = AddInitializerFromArray($"{prefix}.bias", dense.Bias);
        return AddAddNode(matmulOut, biasName, $"{prefix}.output");
    }

    /// <summary>
    ///     导出 Embedding 层（映射为 Gather 算子）
    ///     output = Gather(Weight, input_ids, axis=0)
    /// </summary>
    /// <param name="embedding">嵌入层</param>
    /// <param name="inputName">输入 token ID 张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportEmbedding(Embedding embedding, string inputName, string prefix)
    {
        var weightName = AddInitializerFromArray($"{prefix}.weight", embedding.Weight);
        return AddGatherNode(inputName, weightName, 0, $"{prefix}.output");
    }

    /// <summary>
    ///     导出 Conv2D 层（映射为 Conv 算子）
    ///     将 Galatea 的 im2col 风格权重转换为 ONNX NCHW 格式
    /// </summary>
    /// <param name="conv">卷积层</param>
    /// <param name="inputName">输入张量名称 [batch, inChannels*inH*inW]</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称 [batch, outChannels*outH*outW]</returns>
    private string ExportConv2D(Conv2D conv, string inputName, int inH, int inW, string prefix)
    {
        var batchSize = -1;
        var reshapedInput = AddReshapeNode(inputName, [batchSize, conv.InChannels, inH, inW],
            $"{prefix}.input_nchw");

        var onnxWeightShape = new long[] { conv.OutChannels, conv.InChannels, conv.KernelSize, conv.KernelSize };
        var weightName = AddInitializerFromArrayWithShape($"{prefix}.weight", conv.Weight, onnxWeightShape);

        var biasName = AddInitializerFromArray($"{prefix}.bias", conv.Bias);

        var outputName = UniqueName($"{prefix}.conv_output");
        var attrs = new List<AttrRecord>
        {
            new("kernel_shape", AttrTypeInts, IntsValue: [conv.KernelSize, conv.KernelSize]),
            new("strides", AttrTypeInts, IntsValue: [conv.Stride, conv.Stride]),
            new("pads", AttrTypeInts, IntsValue: [conv.Padding, conv.Padding, conv.Padding, conv.Padding])
        };

        _nodes.Add(new NodeRecord(
            "Conv",
            UniqueName($"{prefix}.conv"),
            [reshapedInput, weightName, biasName],
            [outputName],
            attrs
        ));

        var outH = (inH + 2 * conv.Padding - conv.KernelSize) / conv.Stride + 1;
        var outW = (inW + 2 * conv.Padding - conv.KernelSize) / conv.Stride + 1;
        var flatSize = conv.OutChannels * outH * outW;
        return AddReshapeNode(outputName, [batchSize, flatSize], $"{prefix}.output");
    }

    /// <summary>
    ///     导出 BatchNorm 层（映射为 BatchNormalization 算子）
    /// </summary>
    /// <param name="bn">BatchNorm 层</param>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportBatchNorm(BatchNorm bn, string inputName, string prefix)
    {
        var outputName = UniqueName($"{prefix}.output");
        var scaleName = AddInitializerFromArrayWithShape($"{prefix}.scale", bn.Gamma, [bn._numFeatures]);
        var biasName = AddInitializerFromArrayWithShape($"{prefix}.bias", bn.Beta, [bn._numFeatures]);
        var meanName = AddFloatInitializer($"{prefix}.running_mean", bn._runningMean, [bn._numFeatures]);
        var varName = AddFloatInitializer($"{prefix}.running_var", bn._runningVar, [bn._numFeatures]);

        var attrs = new List<AttrRecord>
        {
            new("epsilon", AttrTypeFloat, bn._epsilon),
            new("momentum", AttrTypeFloat, bn.Momentum)
        };

        _nodes.Add(new NodeRecord(
            "BatchNormalization",
            UniqueName($"{prefix}.batchnorm"),
            [inputName, scaleName, biasName, meanName, varName],
            [outputName],
            attrs
        ));

        return outputName;
    }

    #endregion

    #region 激活函数导出

    /// <summary>
    ///     导出 SiLU 激活函数（分解为 Mul + Sigmoid，因为 opset 17 无原生 Silu）
    ///     silu(x) = x * sigmoid(x)
    /// </summary>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportSiLU(string inputName, string prefix)
    {
        var sigmoidOut = AddSigmoidNode(inputName, $"{prefix}.sigmoid");
        return AddMulNode(inputName, sigmoidOut, $"{prefix}.silu");
    }

    /// <summary>
    ///     导出 GELU 激活函数（分解为 Erf + Mul + Add）
    ///     gelu(x) = 0.5 * x * (1 + Erf(x / sqrt(2)))
    /// </summary>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportGELU(string inputName, string prefix)
    {
        var sqrt2 = MathF.Sqrt(2.0f);
        var invSqrt2Name = AddScalarInitializer($"{prefix}.inv_sqrt2", 1.0f / sqrt2);
        var xScaled = AddMulNode(inputName, invSqrt2Name, $"{prefix}.x_scaled");

        var erfOut = AddErfNode(xScaled, $"{prefix}.erf");

        var oneName = AddScalarInitializer($"{prefix}.one", 1.0f);
        var onePlusErf = AddAddNode(erfOut, oneName, $"{prefix}.one_plus_erf");

        var halfName = AddScalarInitializer($"{prefix}.half", 0.5f);
        var halfX = AddMulNode(inputName, halfName, $"{prefix}.half_x");

        return AddMulNode(halfX, onePlusErf, $"{prefix}.gelu");
    }

    /// <summary>
    ///     导出 ReLU 激活函数（映射为 Relu 算子）
    /// </summary>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportReLU(string inputName, string prefix)
    {
        var outputName = UniqueName($"{prefix}.relu");
        _nodes.Add(new NodeRecord(
            "Relu",
            UniqueName($"{prefix}.relu_node"),
            [inputName],
            [outputName],
            []
        ));
        return outputName;
    }

    /// <summary>
    ///     导出 Sigmoid 激活函数
    /// </summary>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportSigmoid(string inputName, string prefix)
    {
        return AddSigmoidNode(inputName, $"{prefix}.sigmoid");
    }

    /// <summary>
    ///     导出 Tanh 激活函数
    /// </summary>
    /// <param name="inputName">输入张量名称</param>
    /// <param name="prefix">命名前缀</param>
    /// <returns>输出张量名称</returns>
    private string ExportTanh(string inputName, string prefix)
    {
        var outputName = UniqueName($"{prefix}.tanh");
        _nodes.Add(new NodeRecord(
            "Tanh",
            UniqueName($"{prefix}.tanh_node"),
            [inputName],
            [outputName],
            []
        ));
        return outputName;
    }

    #endregion

    #region ONNX 算子节点构建

    /// <summary>
    ///     添加 MatMul 节点
    /// </summary>
    /// <param name="aName">第一个输入名称</param>
    /// <param name="bName">第二个输入名称</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddMatMulNode(string aName, string bName, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        _nodes.Add(new NodeRecord(
            "MatMul",
            UniqueName($"{outputPrefix}_matmul"),
            [aName, bName],
            [outputName],
            []
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Add 节点
    /// </summary>
    /// <param name="aName">第一个输入名称</param>
    /// <param name="bName">第二个输入名称</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddAddNode(string aName, string bName, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        _nodes.Add(new NodeRecord(
            "Add",
            UniqueName($"{outputPrefix}_add"),
            [aName, bName],
            [outputName],
            []
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Mul 节点
    /// </summary>
    /// <param name="aName">第一个输入名称</param>
    /// <param name="bName">第二个输入名称</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddMulNode(string aName, string bName, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        _nodes.Add(new NodeRecord(
            "Mul",
            UniqueName($"{outputPrefix}_mul"),
            [aName, bName],
            [outputName],
            []
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Transpose 节点
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="perm">转置排列</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddTransposeNode(string inputName, long[] perm, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        var attrs = new List<AttrRecord>
        {
            new("perm", AttrTypeInts, IntsValue: perm)
        };

        _nodes.Add(new NodeRecord(
            "Transpose",
            UniqueName($"{outputPrefix}_transpose"),
            [inputName],
            [outputName],
            attrs
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Reshape 节点
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="shape">目标形状</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddReshapeNode(string inputName, long[] shape, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        var shapeName = AddInt64Initializer($"{outputPrefix}_shape", shape, [shape.Length]);

        var attrs = new List<AttrRecord>
        {
            new("allowzero", AttrTypeInt, IntValue: 0)
        };

        _nodes.Add(new NodeRecord(
            "Reshape",
            UniqueName($"{outputPrefix}_reshape"),
            [inputName, shapeName],
            [outputName],
            attrs
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Gather 节点
    /// </summary>
    /// <param name="dataName">数据张量名称</param>
    /// <param name="indicesName">索引张量名称</param>
    /// <param name="axis">采集轴</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddGatherNode(string dataName, string indicesName, int axis, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        var attrs = new List<AttrRecord>
        {
            new("axis", AttrTypeInt, IntValue: axis)
        };

        _nodes.Add(new NodeRecord(
            "Gather",
            UniqueName($"{outputPrefix}_gather"),
            [dataName, indicesName],
            [outputName],
            attrs
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Softmax 节点
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="axis">softmax 轴</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddSoftmaxNode(string inputName, int axis, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        var attrs = new List<AttrRecord>
        {
            new("axis", AttrTypeInt, IntValue: axis)
        };

        _nodes.Add(new NodeRecord(
            "Softmax",
            UniqueName($"{outputPrefix}_softmax"),
            [inputName],
            [outputName],
            attrs
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Split 节点，返回各输出名称的元组
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="axis">分割轴</param>
    /// <param name="splitSizes">各段大小</param>
    /// <param name="out0Prefix">第一个输出前缀</param>
    /// <param name="out1Prefix">第二个输出前缀</param>
    /// <param name="out2Prefix">第三个输出前缀</param>
    /// <returns>三个输出张量名称</returns>
    private (string output0, string output1, string output2) AddSplitNode(
        string inputName, int axis, long[] splitSizes,
        string out0Prefix, string out1Prefix, string out2Prefix)
    {
        var out0 = UniqueName(out0Prefix);
        var out1 = UniqueName(out1Prefix);
        var out2 = UniqueName(out2Prefix);

        var splitName = AddInt64Initializer($"{inputName}_split_sizes", splitSizes, [splitSizes.Length]);

        var attrs = new List<AttrRecord>
        {
            new("axis", AttrTypeInt, IntValue: axis)
        };

        _nodes.Add(new NodeRecord(
            "Split",
            UniqueName($"{inputName}_split"),
            [inputName, splitName],
            [out0, out1, out2],
            attrs
        ));

        return (out0, out1, out2);
    }

    /// <summary>
    ///     添加 ReduceMean 节点
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="axes">归约轴</param>
    /// <param name="keepDims">是否保持维度</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddReduceMeanNode(string inputName, long[] axes, bool keepDims, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        var attrs = new List<AttrRecord>
        {
            new("axes", AttrTypeInts, IntsValue: axes),
            new("keepdims", AttrTypeInt, IntValue: keepDims ? 1 : 0)
        };

        _nodes.Add(new NodeRecord(
            "ReduceMean",
            UniqueName($"{outputPrefix}_reducemean"),
            [inputName],
            [outputName],
            attrs
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Sqrt 节点
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddSqrtNode(string inputName, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        _nodes.Add(new NodeRecord(
            "Sqrt",
            UniqueName($"{outputPrefix}_sqrt"),
            [inputName],
            [outputName],
            []
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Recip 节点（计算 1/x）
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddRecipNode(string inputName, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        _nodes.Add(new NodeRecord(
            "Recip",
            UniqueName($"{outputPrefix}_recip"),
            [inputName],
            [outputName],
            []
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Div 节点
    /// </summary>
    /// <param name="aName">被除数名称</param>
    /// <param name="bName">除数名称</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddDivNode(string aName, string bName, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        _nodes.Add(new NodeRecord(
            "Div",
            UniqueName($"{outputPrefix}_div"),
            [aName, bName],
            [outputName],
            []
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Sigmoid 节点
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddSigmoidNode(string inputName, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        _nodes.Add(new NodeRecord(
            "Sigmoid",
            UniqueName($"{outputPrefix}_sigmoid"),
            [inputName],
            [outputName],
            []
        ));
        return outputName;
    }

    /// <summary>
    ///     添加 Erf 节点
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddErfNode(string inputName, string outputPrefix)
    {
        var outputName = UniqueName(outputPrefix);
        _nodes.Add(new NodeRecord(
            "Erf",
            UniqueName($"{outputPrefix}_erf"),
            [inputName],
            [outputName],
            []
        ));
        return outputName;
    }

    /// <summary>
    ///     先 Reshape 再 Transpose 的组合操作
    ///     用于将 [batch, seqLen, numHeads*dK] 转为 [batch, numHeads, seqLen, dK]
    /// </summary>
    /// <param name="inputName">输入名称</param>
    /// <param name="reshapeTarget">Reshape 目标形状</param>
    /// <param name="perm">Transpose 排列</param>
    /// <param name="outputPrefix">输出名称前缀</param>
    /// <returns>输出张量名称</returns>
    private string AddReshapeThenTranspose(string inputName, long[] reshapeTarget, long[] perm, string outputPrefix)
    {
        var reshaped = AddReshapeNode(inputName, reshapeTarget, $"{outputPrefix}_reshaped");
        return AddTransposeNode(reshaped, perm, outputPrefix);
    }

    #endregion

    #region ONNX Initializer 构建

    /// <summary>
    ///     从 ArrayND 创建 float 类型的 Initializer
    /// </summary>
    /// <param name="name">初始化器名称</param>
    /// <param name="array">源张量数据</param>
    /// <returns>初始化器名称</returns>
    private string AddInitializerFromArray(string name, ArrayND array)
    {
        var safeName = UniqueName(name);
        var dims = new List<long>();
        foreach (var dim in array.Shape) dims.Add(dim);

        var span = array.AsSpan();
        var floatData = new float[span.Length];
        span.CopyTo(floatData);

        _initializers.Add(new InitializerRecord(safeName, OnnxFloat, dims, floatData));
        return safeName;
    }

    /// <summary>
    ///     从 ArrayND 创建 float 类型的 Initializer，并使用指定形状覆盖
    /// </summary>
    /// <param name="name">初始化器名称</param>
    /// <param name="array">源张量数据</param>
    /// <param name="shape">目标形状</param>
    /// <returns>初始化器名称</returns>
    private string AddInitializerFromArrayWithShape(string name, ArrayND array, long[] shape)
    {
        var safeName = UniqueName(name);
        var dims = new List<long>();
        foreach (var dim in shape) dims.Add(dim);

        var span = array.AsSpan();
        var floatData = new float[span.Length];
        span.CopyTo(floatData);

        _initializers.Add(new InitializerRecord(safeName, OnnxFloat, dims, floatData));
        return safeName;
    }

    /// <summary>
    ///     创建 float 标量 Initializer（形状 [1]）
    /// </summary>
    /// <param name="name">初始化器名称</param>
    /// <param name="value">标量值</param>
    /// <returns>初始化器名称</returns>
    private string AddScalarInitializer(string name, float value)
    {
        var safeName = UniqueName(name);
        _initializers.Add(new InitializerRecord(safeName, OnnxFloat, [1], [value]));
        return safeName;
    }

    /// <summary>
    ///     创建 float 数组 Initializer
    /// </summary>
    /// <param name="name">初始化器名称</param>
    /// <param name="data">浮点数据</param>
    /// <param name="shape">张量形状</param>
    /// <returns>初始化器名称</returns>
    private string AddFloatInitializer(string name, float[] data, long[] shape)
    {
        var safeName = UniqueName(name);
        var dims = new List<long>();
        foreach (var dim in shape) dims.Add(dim);

        _initializers.Add(new InitializerRecord(safeName, OnnxFloat, dims, (float[])data.Clone()));
        return safeName;
    }

    /// <summary>
    ///     创建 int64 数组 Initializer
    /// </summary>
    /// <param name="name">初始化器名称</param>
    /// <param name="data">int64 数据</param>
    /// <param name="shape">张量形状</param>
    /// <returns>初始化器名称</returns>
    private string AddInt64Initializer(string name, long[] data, long[] shape)
    {
        var safeName = UniqueName(name);
        var dims = new List<long>();
        foreach (var dim in shape) dims.Add(dim);

        _initializers.Add(new InitializerRecord(safeName, OnnxInt64, dims, Int64Data: (long[])data.Clone()));
        return safeName;
    }

    /// <summary>
    ///     创建因果掩码 Initializer
    ///     形状 [1, 1, seqLen, seqLen]，下三角为 0，上三角为 -1e9
    ///     可广播到 [batch, numHeads, seqLen, seqLen]
    /// </summary>
    /// <param name="seqLen">序列长度</param>
    /// <param name="name">初始化器名称</param>
    /// <returns>初始化器名称</returns>
    private string CreateCausalMaskInitializer(int seqLen, string name)
    {
        var safeName = UniqueName(name);
        var totalSize = seqLen * seqLen;
        var maskData = new float[totalSize];

        for (var i = 0; i < seqLen; i++)
        for (var j = 0; j < seqLen; j++)
            maskData[i * seqLen + j] = j > i ? -1e9f : 0.0f;

        var dims = new List<long> { 1, 1, seqLen, seqLen };
        _initializers.Add(new InitializerRecord(safeName, OnnxFloat, dims, maskData));
        return safeName;
    }

    #endregion

    #region ONNX 图输入输出

    /// <summary>
    ///     添加图输入
    /// </summary>
    /// <param name="name">输入名称</param>
    /// <param name="elemType">元素数据类型</param>
    /// <param name="shape">输入形状</param>
    private void AddInput(string name, int elemType, params long[] shape)
    {
        _inputs.Add(MakeValueInfo(name, elemType, shape));
    }

    /// <summary>
    ///     添加图输出
    /// </summary>
    /// <param name="name">输出名称</param>
    /// <param name="elemType">元素数据类型</param>
    /// <param name="shape">输出形状</param>
    private void AddOutput(string name, int elemType, params long[] shape)
    {
        _outputs.Add(MakeValueInfo(name, elemType, shape));
    }

    /// <summary>
    ///     创建 ValueInfo 记录
    /// </summary>
    /// <param name="name">张量名称</param>
    /// <param name="elemType">元素数据类型</param>
    /// <param name="shape">张量形状</param>
    /// <returns>ValueInfo 记录</returns>
    private static ValueInfoRecord MakeValueInfo(string name, int elemType, long[] shape)
    {
        return new ValueInfoRecord(name, elemType, shape);
    }

    #endregion

    #region ONNX 辅助方法

    /// <summary>
    ///     生成唯一名称，避免 ONNX 图中名称冲突
    /// </summary>
    /// <param name="baseName">基础名称</param>
    /// <returns>唯一名称</returns>
    private string UniqueName(string baseName)
    {
        if (_usedNames.Add(baseName)) return baseName;

        var name = $"{baseName}_{_nameCounter++}";
        while (!_usedNames.Add(name)) name = $"{baseName}_{_nameCounter++}";

        return name;
    }

    /// <summary>
    ///     构建 ONNX ModelProto 的 protobuf 字节数组
    /// </summary>
    /// <returns>protobuf 编码的 ONNX 模型字节数组</returns>
    private byte[] BuildModelBytes()
    {
        var writer = new OnnxProtoWriter();

        WriteModelProto(writer);

        return writer.ToArray();
    }

    /// <summary>
    ///     将模型写入 .onnx 文件
    /// </summary>
    /// <param name="outputPath">输出文件路径</param>
    private void WriteToFile(string outputPath)
    {
        var bytes = BuildModelBytes();
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        File.WriteAllBytes(outputPath, bytes);
    }

    #endregion

    #region Protobuf 序列化

    /// <summary>
    ///     写入 ModelProto 消息
    ///     字段：ir_version=1, producer_name=2, producer_version=3, domain=7, model_version=5, doc_string=6, opset_import=8,
    ///     graph=100
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    private void WriteModelProto(OnnxProtoWriter w)
    {
        w.WriteInt64(1, IrVersion);
        w.WriteString(2, "Galatea");
        w.WriteString(3, "0.1.0");
        w.WriteString(7, "ai.galatea");
        w.WriteInt64(5, 1);
        w.WriteString(6, "Galatea ONNX Export");

        w.WriteMessage(8, WriteOpsetImport);

        w.WriteMessage(100, WriteGraphProto);
    }

    /// <summary>
    ///     写入 OperatorSetIdProto 消息
    ///     字段：domain=1, version=2
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    private void WriteOpsetImport(OnnxProtoWriter w)
    {
        w.WriteString(1, "");
        w.WriteInt64(2, OpsetVersion);
    }

    /// <summary>
    ///     写入 GraphProto 消息
    ///     字段：node=1, name=2, initializer=5, doc_string=14, input=11, output=12, value_info=13
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    private void WriteGraphProto(OnnxProtoWriter w)
    {
        foreach (var node in _nodes) w.WriteMessage(1, nw => WriteNodeProto(nw, node));

        w.WriteString(2, _graphName);

        foreach (var init in _initializers) w.WriteMessage(5, nw => WriteTensorProto(nw, init));

        w.WriteString(14, "");

        foreach (var input in _inputs) w.WriteMessage(11, nw => WriteValueInfoProto(nw, input));

        foreach (var output in _outputs) w.WriteMessage(12, nw => WriteValueInfoProto(nw, output));
    }

    /// <summary>
    ///     写入 NodeProto 消息
    ///     字段：input=1, output=2, name=3, op_type=4, attribute=5, doc_string=6, domain=7
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    /// <param name="node">节点记录</param>
    private static void WriteNodeProto(OnnxProtoWriter w, NodeRecord node)
    {
        foreach (var input in node.Inputs) w.WriteString(1, input);

        foreach (var output in node.Outputs) w.WriteString(2, output);

        w.WriteString(3, node.Name);
        w.WriteString(4, node.OpType);

        foreach (var attr in node.Attributes) w.WriteMessage(5, aw => WriteAttributeProto(aw, attr));

        w.WriteString(6, "");
        w.WriteString(7, "");
    }

    /// <summary>
    ///     写入 AttributeProto 消息
    ///     字段：name=1, f=2, i=3, s=4, t=5, g=6, floats=7, ints=8, strings=9, tensors=10, graphs=11, doc_string=13, type=20,
    ///     ref_attr_name=21
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    /// <param name="attr">属性记录</param>
    private static void WriteAttributeProto(OnnxProtoWriter w, AttrRecord attr)
    {
        w.WriteString(1, attr.Name);

        switch (attr.type)
        {
            case AttrTypeFloat:
                w.WriteFloat(2, attr.FloatValue);
                break;
            case AttrTypeInt:
                w.WriteInt64(3, attr.IntValue);
                break;
        }

        w.WriteEnum(20, attr.type);

        if (attr.type == AttrTypeInts && attr.IntsValue != null)
            foreach (var v in attr.IntsValue)
                w.WriteInt64(8, v);
    }

    /// <summary>
    ///     写入 TensorProto 消息
    ///     字段：dims=1, data_type=2, float_data=3, int32_data=4, int64_data=5, raw_data=6, name=7, doc_string=8
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    /// <param name="init">初始化器记录</param>
    private static void WriteTensorProto(OnnxProtoWriter w, InitializerRecord init)
    {
        foreach (var dim in init.Dims) w.WriteInt64(1, dim);

        w.WriteEnum(2, init.DataType);

        if (init.FloatData != null && init.FloatData.Length > 0) w.WriteRepeatedFloat(3, init.FloatData);

        if (init.Int64Data != null && init.Int64Data.Length > 0) w.WriteRepeatedInt64(5, init.Int64Data);

        w.WriteString(7, init.Name);
        w.WriteString(8, "");
    }

    /// <summary>
    ///     写入 ValueInfoProto 消息
    ///     字段：name=1, type=2, doc_string=3
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    /// <param name="vi">值信息记录</param>
    private static void WriteValueInfoProto(OnnxProtoWriter w, ValueInfoRecord vi)
    {
        w.WriteString(1, vi.Name);

        w.WriteMessage(2, tw => WriteTypeProto(tw, vi.ElemType, vi.Shape));

        w.WriteString(3, "");
    }

    /// <summary>
    ///     写入 TypeProto 消息
    ///     字段：tensor_type=1
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    /// <param name="elemType">元素类型</param>
    /// <param name="shape">张量形状</param>
    private static void WriteTypeProto(OnnxProtoWriter w, int elemType, long[] shape)
    {
        w.WriteMessage(1, ttw => WriteTensorTypeProto(ttw, elemType, shape));
    }

    /// <summary>
    ///     写入 TypeProto.Tensor 消息
    ///     字段：elem_type=1, shape=2
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    /// <param name="elemType">元素类型</param>
    /// <param name="shape">张量形状</param>
    private static void WriteTensorTypeProto(OnnxProtoWriter w, int elemType, long[] shape)
    {
        w.WriteEnum(1, elemType);

        w.WriteMessage(2, sw => WriteTensorShapeProto(sw, shape));
    }

    /// <summary>
    ///     写入 TensorShapeProto 消息
    ///     字段：dim=1
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    /// <param name="shape">张量形状</param>
    private static void WriteTensorShapeProto(OnnxProtoWriter w, long[] shape)
    {
        foreach (var dim in shape) w.WriteMessage(1, dw => WriteDimensionProto(dw, dim));
    }

    /// <summary>
    ///     写入 TensorShapeProto.Dimension 消息
    ///     字段：dim_value=1, dim_param=2
    /// </summary>
    /// <param name="w">protobuf 写入器</param>
    /// <param name="dim">维度值</param>
    private static void WriteDimensionProto(OnnxProtoWriter w, long dim)
    {
        if (dim > 0)
            w.WriteInt64(1, dim);
        else
            w.WriteString(2, "dynamic");
    }

    #endregion

    #region 内部记录类型

    /// <summary>
    ///     ONNX 节点记录
    /// </summary>
    /// <param name="OpType">算子类型</param>
    /// <param name="Name">节点名称</param>
    /// <param name="Inputs">输入名称列表</param>
    /// <param name="Outputs">输出名称列表</param>
    /// <param name="Attributes">属性列表</param>
    private sealed record NodeRecord(
        string OpType,
        string Name,
        List<string> Inputs,
        List<string> Outputs,
        List<AttrRecord> Attributes
    );

    /// <summary>
    ///     ONNX 属性记录
    /// </summary>
    /// <param name="Name">属性名称</param>
    /// <param name="Type">属性类型</param>
    /// <param name="FloatValue">浮点值（FLOAT 类型）</param>
    /// <param name="IntValue">整数值（INT 类型）</param>
    /// <param name="IntsValue">整数列表值（INTS 类型）</param>
    private sealed record AttrRecord(
        string Name,
        int Type,
        float FloatValue = 0.0f,
        long IntValue = 0,
        long[]? IntsValue = null
    );

    /// <summary>
    ///     ONNX 初始化器（TensorProto）记录
    /// </summary>
    /// <param name="Name">张量名称</param>
    /// <param name="DataType">数据类型</param>
    /// <param name="Dims">维度列表</param>
    /// <param name="FloatData">浮点数据</param>
    /// <param name="Int64Data">int64 数据</param>
    private sealed record InitializerRecord(
        string Name,
        int DataType,
        List<long> Dims,
        float[]? FloatData = null,
        long[]? Int64Data = null
    );

    /// <summary>
    ///     ONNX 值信息（ValueInfoProto）记录
    /// </summary>
    /// <param name="Name">张量名称</param>
    /// <param name="ElemType">元素类型</param>
    /// <param name="Shape">张量形状</param>
    private sealed record ValueInfoRecord(
        string Name,
        int ElemType,
        long[] Shape
    );

    #endregion
}
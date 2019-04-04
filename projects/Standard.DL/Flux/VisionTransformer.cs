using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     Vision Transformer (ViT) 模型 —— Patch Embedding + Transformer Encoder + Classification Head
///     结构：Conv2D(patchEmbedding) → flatten → CLS Token → Position Embedding → N × BertEncoderLayer → LayerNorm →
///     Dense(numClasses)
///     输入：展平图像 [batch, imageChannels * imageSize * imageSize]
///     输出：分类 logits [batch, numClasses]
/// </summary>
public sealed class VisionTransformer : ITrainableModel
{
    #region 构造函数

    /// <summary>
    ///     创建 Vision Transformer 模型
    /// </summary>
    /// <param name="numClasses">分类数</param>
    /// <param name="imageChannels">输入图像通道数</param>
    /// <param name="imageSize">输入图像尺寸（正方形）</param>
    /// <param name="patchSize">Patch 尺寸</param>
    /// <param name="dModel">模型维度</param>
    /// <param name="numHeads">注意力头数</param>
    /// <param name="numLayers">编码器层数</param>
    /// <param name="dFFN">前馈网络隐藏维度</param>
    /// <param name="dropoutRate">Dropout 丢弃率</param>
    public VisionTransformer(
        int numClasses,
        int imageChannels = 3,
        int imageSize = 224,
        int patchSize = 16,
        int dModel = 768,
        int numHeads = 12,
        int numLayers = 12,
        int dFFN = 3072,
        float dropoutRate = 0.1f)
    {
        NumClasses = numClasses;
        ImageChannels = imageChannels;
        ImageSize = imageSize;
        PatchSize = patchSize;
        DModel = dModel;
        NumLayers = numLayers;

        NumPatches = imageSize / patchSize * (imageSize / patchSize);
        _seqLen = NumPatches + 1;

        PatchEmbedding = new Conv2D(
            imageChannels, dModel, patchSize,
            patchSize, 0,
            imageSize, imageSize);

        ClsToken = ArrayND.HeNormal(dModel, 1, 1, dModel);
        PosEmbedding = ArrayND.HeNormal(dModel, 1, _seqLen, dModel);

        _layers = new BertEncoderLayer[numLayers];
        for (var i = 0; i < numLayers; i++) _layers[i] = new BertEncoderLayer(dModel, numHeads, dFFN);

        ClsNorm = new LayerNorm(dModel);
        ClassificationHead = new Dense(dModel, numClasses);
        EmbeddingDropout = new Dropout(dropoutRate);
    }

    #endregion

    #region 辅助方法

    private static ArrayND AddArraysWithGrad(ArrayND a, ArrayND b, AutogradContext ctx)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] + spanB[i];

        ctx.Record(result, [a, b], outputGrads =>
        {
            var dSum = outputGrads[0];
            return [dSum, dSum];
        });

        return result;
    }

    #endregion

    #region 字段

    private readonly BertEncoderLayer[] _layers;

    private readonly int _seqLen;

    #endregion

    #region 属性

    /// <summary>
    ///     Patch 嵌入卷积层
    /// </summary>
    public Conv2D PatchEmbedding { get; }

    /// <summary>
    ///     可学习的 CLS Token [1, 1, dModel]
    /// </summary>
    public ArrayND ClsToken { get; }

    /// <summary>
    ///     可学习的位置嵌入 [1, numPatches+1, dModel]
    /// </summary>
    public ArrayND PosEmbedding { get; }

    /// <summary>
    ///     Transformer 编码器层
    /// </summary>
    public ReadOnlySpan<BertEncoderLayer> Layers => _layers;

    /// <summary>
    ///     CLS Token 归一化层
    /// </summary>
    public LayerNorm ClsNorm { get; }

    /// <summary>
    ///     分类头全连接层
    /// </summary>
    public Dense ClassificationHead { get; }

    /// <summary>
    ///     嵌入层 Dropout
    /// </summary>
    public Dropout EmbeddingDropout { get; }

    /// <summary>
    ///     分类数
    /// </summary>
    public int NumClasses { get; }

    /// <summary>
    ///     输入图像通道数
    /// </summary>
    public int ImageChannels { get; }

    /// <summary>
    ///     输入图像尺寸
    /// </summary>
    public int ImageSize { get; }

    /// <summary>
    ///     Patch 尺寸
    /// </summary>
    public int PatchSize { get; }

    /// <summary>
    ///     模型维度
    /// </summary>
    public int DModel { get; }

    /// <summary>
    ///     编码器层数
    /// </summary>
    public int NumLayers { get; }

    /// <summary>
    ///     Patch 数量
    /// </summary>
    public int NumPatches { get; }

    #endregion

    #region ITrainableModel 实现

    /// <summary>
    ///     前向传播：展平图像 → 分类 logits
    /// </summary>
    /// <param name="input">展平图像 [batch, imageChannels * imageSize * imageSize]</param>
    /// <returns>分类 logits [batch, numClasses]</returns>
    public ArrayND forward(ArrayND input)
    {
        var batch = input.Shape[0];

        var (convOutput, _, _) = PatchEmbedding.Forward(input, ImageSize, ImageSize);
        var patchEmb = ReshapeAndTransposePatch(convOutput, batch);

        var clsTokens = ExpandClsToken(batch);
        var embeddings = ArrayND.Concat(1, clsTokens, patchEmb);
        var x = embeddings + PosEmbedding;
        x = EmbeddingDropout.Forward(x);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x);

        var clsOutput = x.Slice(1, 0, 1).Reshape(batch, DModel);
        clsOutput = ClsNorm.Forward(clsOutput);
        return ClassificationHead.forward(clsOutput);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="input">展平图像 [batch, imageChannels * imageSize * imageSize]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>分类 logits [batch, numClasses]</returns>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var batch = input.Shape[0];

        var (convOutput, _, _) = PatchEmbedding.Forward(input, ImageSize, ImageSize, ctx);
        var patchEmb = ReshapeAndTransposePatchWithGrad(convOutput, batch, ctx);

        var clsTokens = ExpandClsTokenWithGrad(batch, ctx);
        var embeddings = ArrayND.ConcatWithGrad(1, ctx, clsTokens, patchEmb);
        var x = AddArraysWithGrad(embeddings, PosEmbedding, ctx);

        for (var i = 0; i < _layers.Length; i++) x = _layers[i].forward(x, ctx);

        var clsOutput = ExtractClsToken(x, batch, ctx);
        clsOutput = ClsNorm.Forward(clsOutput, ctx);
        return ClassificationHead.forward(clsOutput, ctx);
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in PatchEmbedding.Parameters()) yield return p;

        yield return new Parameter(ClsToken);
        yield return new Parameter(PosEmbedding);
        for (var i = 0; i < _layers.Length; i++)
            foreach (var p in _layers[i].Parameters())
                yield return p;

        foreach (var p in ClsNorm.Parameters()) yield return p;

        foreach (var p in ClassificationHead.Parameters()) yield return p;
    }

    #endregion

    #region Patch 嵌入变换

    /// <summary>
    ///     将 Conv2D 输出重塑并转置为 Patch 嵌入 [batch, numPatches, dModel]
    ///     Conv2D 输出为 NCHW 展平格式 [batch, dModel * numPatches]
    /// </summary>
    /// <param name="convOutput">Conv2D 输出 [batch, dModel * numPatches]</param>
    /// <param name="batch">批次大小</param>
    /// <returns>Patch 嵌入 [batch, numPatches, dModel]</returns>
    private ArrayND ReshapeAndTransposePatch(ArrayND convOutput, int batch)
    {
        var reshaped = convOutput.Reshape(batch, DModel, NumPatches);
        return reshaped.Transpose(1, 2);
    }

    /// <summary>
    ///     将 Conv2D 输出重塑并转置为 Patch 嵌入（带自动微分记录）
    /// </summary>
    /// <param name="convOutput">Conv2D 输出 [batch, dModel * numPatches]</param>
    /// <param name="batch">批次大小</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>Patch 嵌入 [batch, numPatches, dModel]</returns>
    private ArrayND ReshapeAndTransposePatchWithGrad(ArrayND convOutput, int batch, AutogradContext ctx)
    {
        var reshaped = convOutput.ReshapeWithGrad([batch, DModel, NumPatches], ctx);
        return TransposeWithGrad(reshaped, 1, 2, ctx);
    }

    /// <summary>
    ///     转置两个轴（带自动微分记录）
    ///     转置的梯度是再次转置（转置是自身的逆操作）
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <param name="dim0">第一个轴</param>
    /// <param name="dim1">第二个轴</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>转置后的张量</returns>
    private static ArrayND TransposeWithGrad(ArrayND input, int dim0, int dim1, AutogradContext ctx)
    {
        var result = input.Transpose(dim0, dim1);

        ctx.Record(result, [input], outputGrads =>
        {
            var dResult = outputGrads[0];
            var dInput = dResult.Transpose(dim0, dim1);
            return [dInput];
        });

        return result;
    }

    #endregion

    #region CLS Token 操作

    /// <summary>
    ///     将 CLS Token 从 [1, 1, dModel] 扩展为 [batch, 1, dModel]
    /// </summary>
    /// <param name="batch">批次大小</param>
    /// <returns>扩展后的 CLS Token [batch, 1, dModel]</returns>
    private ArrayND ExpandClsToken(int batch)
    {
        var expanded = ArrayND.Zeros(batch, 1, DModel);
        var spanSrc = ClsToken.AsSpan();
        var spanDst = expanded.AsWriteSpan();
        for (var b = 0; b < batch; b++)
        {
            var dstOff = b * DModel;
            for (var d = 0; d < DModel; d++) spanDst[dstOff + d] = spanSrc[d];
        }

        return expanded;
    }

    /// <summary>
    ///     将 CLS Token 从 [1, 1, dModel] 扩展为 [batch, 1, dModel]（带自动微分记录）
    ///     梯度：将所有批次的梯度累加回原始 CLS Token
    /// </summary>
    /// <param name="batch">批次大小</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>扩展后的 CLS Token [batch, 1, dModel]</returns>
    private ArrayND ExpandClsTokenWithGrad(int batch, AutogradContext ctx)
    {
        var expanded = ExpandClsToken(batch);

        ctx.Record(expanded, [ClsToken], outputGrads =>
        {
            var dExpanded = outputGrads[0];
            var dClsToken = ArrayND.Zeros(1, 1, DModel);
            var spanDE = dExpanded.AsSpan();
            var spanDC = dClsToken.AsWriteSpan();

            for (var b = 0; b < batch; b++)
            {
                var srcOff = b * DModel;
                for (var d = 0; d < DModel; d++) spanDC[d] += spanDE[srcOff + d];
            }

            return [dClsToken];
        });

        return expanded;
    }

    /// <summary>
    ///     从 Transformer 输出中提取 CLS Token（带自动微分记录）
    ///     提取第一个位置 [batch, 0, :] → [batch, dModel]
    /// </summary>
    /// <param name="sequenceOutput">序列输出 [batch, seqLen, dModel]</param>
    /// <param name="batch">批次大小</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>CLS Token 输出 [batch, dModel]</returns>
    private static ArrayND ExtractClsToken(ArrayND sequenceOutput, int batch, AutogradContext ctx)
    {
        var seqLen = sequenceOutput.Shape[1];
        var dModel = sequenceOutput.Shape[2];
        var clsOutput = ArrayND.Zeros(batch, dModel);
        var spanSrc = sequenceOutput.AsSpan();
        var spanDst = clsOutput.AsWriteSpan();

        for (var b = 0; b < batch; b++)
        {
            var srcOff = b * seqLen * dModel;
            var dstOff = b * dModel;
            for (var d = 0; d < dModel; d++) spanDst[dstOff + d] = spanSrc[srcOff + d];
        }

        ctx.Record(clsOutput, [sequenceOutput], outputGrads =>
        {
            var dCls = outputGrads[0];
            var dSeq = ArrayND.Zeros(batch, seqLen, dModel);
            var spanDCls = dCls.AsSpan();
            var spanDSeq = dSeq.AsWriteSpan();

            for (var b = 0; b < batch; b++)
            {
                var dstOff = b * seqLen * dModel;
                var srcOff = b * dModel;
                for (var d = 0; d < dModel; d++) spanDSeq[dstOff + d] = spanDCls[srcOff + d];
            }

            return [dSeq];
        });

        return clsOutput;
    }

    #endregion
}
using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     LoRA（Low-Rank Adaptation）低秩适配器 —— 参数高效微调
///     将 W 的更新分解为 ΔW = A × B，其中 A ∈ R^(d×r), B ∈ R^(r×d)
///     只训练 A 和 B（2×r×d 参数），冻结原始 W（d×d 参数）
///     当 r << d 时， 可训练参数量大幅减少
/// </summary>
public sealed class LoRAAdapter : ILayer
{
    private readonly float _alpha;
    private readonly int _fanIn;
    private readonly int _fanOut;

    /// <summary>
    ///     创建 LoRA 适配器
    /// </summary>
    /// <param name="baseLayer">要适配的基础 Dense 层（冻结）</param>
    /// <param name="rank">LoRA 秩（通常 4~64）</param>
    /// <param name="alpha">LoRA 缩放系数（通常等于 rank 或 2×rank）</param>
    public LoRAAdapter(Dense baseLayer, int rank = 8, float alpha = 16.0f)
    {
        BaseLayer = baseLayer;
        Rank = rank;
        _alpha = alpha;
        _fanIn = baseLayer.Weight.Shape[0];
        _fanOut = baseLayer.Weight.Shape[1];
        Scaling = alpha / rank;

        MatrixA = ArrayND.RandomNormal(_fanIn, rank) * 0.01f;
        MatrixB = ArrayND.Zeros(rank, _fanOut);
    }

    /// <summary>
    ///     矩阵 A [fanIn, rank]（降维）
    /// </summary>
    public ArrayND MatrixA { get; }

    /// <summary>
    ///     矩阵 B [rank, fanOut]（升维）
    /// </summary>
    public ArrayND MatrixB { get; }

    /// <summary>
    ///     缩放因子
    /// </summary>
    public float Scaling { get; }

    /// <summary>
    ///     LoRA 秩
    /// </summary>
    public int Rank { get; }

    /// <summary>
    ///     基础层
    /// </summary>
    public Dense BaseLayer { get; }

    /// <summary>
    ///     获取可训练参数（仅 LoRA 的 A 和 B，不含基础层）
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        yield return new Parameter(MatrixA);
        yield return new Parameter(MatrixB);
    }

    /// <summary>
    ///     前向传播：output = input × (W + scaling × A × B) + bias
    /// </summary>
    /// <param name="input">输入 [..., fanIn]</param>
    /// <returns>输出 [..., fanOut]</returns>
    public ArrayND Forward(ArrayND input)
    {
        var baseOutput = BaseLayer.forward(input);

        var loraWeight = ArrayND.MatMul(MatrixA, MatrixB);

        var spanW = loraWeight.AsWriteSpan();
        for (var i = 0; i < spanW.Length; i++) spanW[i] *= Scaling;

        if (input.Shape.Length == 2)
        {
            var loraOutput = ArrayND.MatMul(input, loraWeight);
            return baseOutput + loraOutput;
        }

        var ndim = input.Shape.Length;
        var batchSize = 1;
        for (var d = 0; d < ndim - 1; d++) batchSize *= input.Shape[d];
        var flatInput = input.Reshape(batchSize, _fanIn);
        var flatLoraOutput = ArrayND.MatMul(flatInput, loraWeight);

        var outShape = new int[ndim];
        Array.Copy(input.Shape, outShape, ndim);
        outShape[ndim - 1] = _fanOut;
        var loraOutputReshaped = flatLoraOutput.Reshape(outShape);

        return baseOutput + loraOutputReshaped;
    }

    /// <summary>
    ///     带自动微分的前向传播
    /// </summary>
    public ArrayND Forward(ArrayND input, AutogradContext ctx)
    {
        var baseOutput = BaseLayer.forward(input, ctx);

        var loraWeight = ArrayND.MatMul(MatrixA, MatrixB);

        var savedScaling = Scaling;
        var savedFanIn = _fanIn;
        var savedFanOut = _fanOut;
        var savedInputShape = input.Shape;

        ArrayND loraContribution;
        if (input.Shape.Length == 2)
        {
            loraContribution = ArrayND.MatMul(input, loraWeight);
        }
        else
        {
            var ndim = input.Shape.Length;
            var batchSize = 1;
            for (var d = 0; d < ndim - 1; d++) batchSize *= input.Shape[d];
            var flatInput = input.Reshape(batchSize, savedFanIn);
            var flatLoraOut = ArrayND.MatMul(flatInput, loraWeight);
            var outShape = new int[ndim];
            Array.Copy(input.Shape, outShape, ndim);
            outShape[ndim - 1] = savedFanOut;
            loraContribution = flatLoraOut.Reshape(outShape);
        }

        var scaledLora = loraContribution * savedScaling;

        var output = baseOutput + scaledLora;

        ctx.Record(output, [input, MatrixA, MatrixB], outputGrads =>
        {
            var dOutput = outputGrads[0];

            var dScaledLora = dOutput;
            var dLoraContribution = dScaledLora * savedScaling;

            if (savedInputShape.Length > 2)
            {
                var ndim = savedInputShape.Length;
                var batchSize = 1;
                for (var d = 0; d < ndim - 1; d++) batchSize *= savedInputShape[d];
                dLoraContribution = dLoraContribution.Reshape(batchSize, savedFanOut);
            }

            var flatInputForGrad = savedInputShape.Length > 2
                ? input.Reshape(input.Shape.Aggregate(1, (a, b) => a * b) / savedFanIn, savedFanIn)
                : input;

            var dLoraWeight = ArrayND.MatMul(flatInputForGrad.Transpose(), dLoraContribution);
            var dA = ArrayND.MatMul(dLoraWeight, MatrixB.Transpose());
            var dB = ArrayND.MatMul(MatrixA.Transpose(), dLoraWeight);

            var dInput = ArrayND.MatMul(dLoraContribution, ArrayND.MatMul(MatrixA, MatrixB).Transpose());

            if (savedInputShape.Length > 2) dInput = dInput.Reshape(savedInputShape);

            return [dInput, dA, dB];
        });

        return output;
    }

    /// <summary>
    ///     获取所有参数（包括基础层）
    /// </summary>
    public IEnumerable<IParameter> AllParameters()
    {
        foreach (var p in BaseLayer.Parameters()) yield return p;

        yield return new Parameter(MatrixA);
        yield return new Parameter(MatrixB);
    }

    /// <summary>
    ///     将 LoRA 权重合并回基础层（推理优化）
    ///     合并后 W_new = W + scaling × A × B，不再需要 LoRA 适配器
    /// </summary>
    /// <returns>合并后的 Dense 层</returns>
    public Dense MergeWeights()
    {
        var loraWeight = ArrayND.MatMul(MatrixA, MatrixB);
        var spanLW = loraWeight.AsWriteSpan();
        for (var i = 0; i < spanLW.Length; i++) spanLW[i] *= Scaling;

        var mergedWeight = BaseLayer.Weight + loraWeight;

        var merged = new Dense(_fanIn, _fanOut);
        var spanMW = merged.Weight.AsWriteSpan();
        var spanW = mergedWeight.AsSpan();
        for (var i = 0; i < spanMW.Length; i++) spanMW[i] = spanW[i];

        var spanMB = merged.Bias.AsWriteSpan();
        var spanBB = BaseLayer.Bias.AsSpan();
        for (var i = 0; i < spanMB.Length; i++) spanMB[i] = spanBB[i];

        return merged;
    }
}

/// <summary>
///     LoRA 模型包装器 —— 将 LoRA 适配器应用到 GPT 模型的所有 Dense 层
///     只训练 LoRA 参数，冻结原始模型权重
/// </summary>
public sealed class LoRAModel : ITrainableModel
{
    private readonly List<LoRAAdapter> _adapters;
    private readonly float _alpha;
    private readonly int _rank;

    /// <summary>
    ///     创建 LoRA 模型
    /// </summary>
    /// <param name="baseModel">基础模型</param>
    /// <param name="rank">LoRA 秩</param>
    /// <param name="alpha">LoRA 缩放系数</param>
    /// <param name="targetLayerNames">要应用 LoRA 的层名称（null 表示所有 Dense 层）</param>
    public LoRAModel(ITrainableModel baseModel, int rank = 8, float alpha = 16.0f, string[]? targetLayerNames = null)
    {
        BaseModel = baseModel;
        _rank = rank;
        _alpha = alpha;
        _adapters = [];

        foreach (var param in baseModel.Parameters())
            if (param.Value.Shape is [> 1, > 1])
            {
                var fanIn = param.Value.Shape[0];
                var fanOut = param.Value.Shape[1];
                var adapter = new LoRAAdapter(new Dense(fanIn, fanOut), rank, alpha);
                var spanBW = adapter.BaseLayer.Weight.AsWriteSpan();
                var spanPW = param.Value.AsSpan();
                for (var i = 0; i < spanBW.Length && i < spanPW.Length; i++) spanBW[i] = spanPW[i];
                _adapters.Add(adapter);
            }
    }

    /// <summary>
    ///     基础模型
    /// </summary>
    public ITrainableModel BaseModel { get; }

    /// <summary>
    ///     LoRA 适配器列表
    /// </summary>
    public IReadOnlyList<LoRAAdapter> Adapters => _adapters;

    /// <summary>
    ///     前向传播（委托给基础模型）
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        return BaseModel.forward(input);
    }

    /// <summary>
    ///     带自动微分的前向传播（委托给基础模型）
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        return BaseModel.forward(input, ctx);
    }

    /// <summary>
    ///     获取可训练参数（仅 LoRA 适配器参数，冻结基础模型）
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var adapter in _adapters)
        foreach (var p in adapter.Parameters())
            yield return p;
    }

    /// <summary>
    ///     可训练参数数量
    /// </summary>
    public int TrainableParameterCount()
    {
        var count = 0;
        foreach (var adapter in _adapters)
        {
            count += adapter.MatrixA.Shape.Aggregate(1, (a, b) => a * b);
            count += adapter.MatrixB.Shape.Aggregate(1, (a, b) => a * b);
        }

        return count;
    }

    /// <summary>
    ///     总参数数量（含基础模型）
    /// </summary>
    public int TotalParameterCount()
    {
        return ModelSummary.CountParameters(BaseModel.Parameters()) + TrainableParameterCount();
    }

    /// <summary>
    ///     参数效率比（可训练参数 / 总参数）
    /// </summary>
    public float ParameterEfficiency()
    {
        var total = TotalParameterCount();
        return total > 0 ? (float)TrainableParameterCount() / total : 0;
    }
}
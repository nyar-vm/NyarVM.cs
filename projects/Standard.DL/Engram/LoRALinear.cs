using Std.DL.Flux;
using Std.DL.Training;

namespace Std.DL.Engram;

/// <summary>
///     LoRA 低秩适配层 —— 在冻结的 Dense 层上叠加低秩矩阵乘积 A×B 作为可训练增量
/// </summary>
public sealed class LoRALinear : ILayer, ITrainableModel
{
    /// <summary>
    ///     创建 LoRA 适配层
    /// </summary>
    /// <param name="baseLayer">冻结的基础 Dense 层</param>
    /// <param name="rank">低秩维度</param>
    /// <param name="alpha">缩放因子</param>
    public LoRALinear(Dense baseLayer, int rank = 8, float alpha = 8.0f)
    {
        BaseLayer = baseLayer;
        Rank = rank;
        Alpha = alpha;
        IsMerged = false;

        FanIn = baseLayer.Weight.Shape[0];
        FanOut = baseLayer.Weight.Shape[1];

        WeightA = ArrayND.HeNormal(FanIn, FanIn, Rank);
        WeightB = ArrayND.Zeros(Rank, FanOut);

        var scaleA = 1.0f / MathF.Sqrt(FanIn);
        var spanA = WeightA.AsWriteSpan();
        for (var i = 0; i < spanA.Length; i++) spanA[i] *= scaleA;
    }

    /// <summary>基础层 FanIn</summary>
    public int FanIn { get; }

    /// <summary>基础层 FanOut</summary>
    public int FanOut { get; }

    /// <summary>低秩维度</summary>
    public int Rank { get; }

    /// <summary>缩放因子</summary>
    public float Alpha { get; }

    /// <summary>是否已合并到基础权重</summary>
    public bool IsMerged { get; private set; }

    /// <summary>基础 Dense 层</summary>
    public Dense BaseLayer { get; }

    /// <summary>LoRA A 矩阵 [fanIn, rank]</summary>
    public ArrayND WeightA { get; private set; }

    /// <summary>LoRA B 矩阵 [rank, fanOut]</summary>
    public ArrayND WeightB { get; private set; }

    /// <summary>缩放系数 alpha / rank</summary>
    public float ScaleFactor => Alpha / Rank;

    /// <summary>
    ///     获取 LoRA 参数（A 和 B）—— 仅这些参数参与训练
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        var aParam = new SimpleParameter(WeightA);
        var bParam = new SimpleParameter(WeightB);
        return [aParam, bParam];
    }

    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        var baseOutput = BaseLayer.forward(input);

        if (IsMerged) return baseOutput;

        var loraOutput = ComputeLoraContribution(input);
        return ElementWiseAdd(baseOutput, loraOutput);
    }

    /// <summary>
    ///     前向传播（带自动微分）
    ///     只在 LoRA 参数上累积梯度，基础层梯度不变
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        if (IsMerged) return BaseLayer.forward(input, ctx);

        var baseOutput = BaseLayer.forward(input);
        var loraOutput = ComputeLoraContribution(input, ctx);

        return ElementWiseAdd(baseOutput, loraOutput);
    }

    /// <summary>
    ///     获取所有参数（包括基础层参数）
    /// </summary>
    public IEnumerable<IParameter> AllParameters()
    {
        foreach (var p in BaseLayer.Parameters()) yield return p;

        foreach (var p in Parameters()) yield return p;
    }

    /// <summary>
    ///     获取 LoRA 增量权重矩阵：A × B
    /// </summary>
    public float[] GetIncrementWeights()
    {
        var result = new float[FanIn * FanOut];
        var spanA = WeightA.AsSpan();
        var spanB = WeightB.AsSpan();

        for (var i = 0; i < FanIn; i++)
        for (var o = 0; o < FanOut; o++)
        {
            var sum = 0.0f;
            for (var r = 0; r < Rank; r++) sum += spanA[i * Rank + r] * spanB[r * FanOut + o];
            result[i * FanOut + o] = sum * ScaleFactor;
        }

        return result;
    }

    /// <summary>
    ///     合并 LoRA 增量到基础权重（W_base += alpha/r * A×B），然后清零 A 和 B
    /// </summary>
    public void MergeWeights()
    {
        if (IsMerged) return;

        var increments = GetIncrementWeights();
        var weightSpan = BaseLayer.Weight.AsWriteSpan();

        for (var i = 0; i < weightSpan.Length; i++) weightSpan[i] += increments[i];

        WeightA = ArrayND.Zeros(FanIn, Rank);
        WeightB = ArrayND.Zeros(Rank, FanOut);
        IsMerged = true;
    }

    /// <summary>
    ///     取消合并 —— 从基础权重中减去已合并的 LoRA 增量
    /// </summary>
    public void UnmergeWeights()
    {
        if (!IsMerged) return;

        IsMerged = false;
        WeightA = ArrayND.HeNormal(FanIn, FanIn, Rank);
        var rscaleA = 1.0f / MathF.Sqrt(FanIn);
        var rspanA = WeightA.AsWriteSpan();
        for (var i = 0; i < rspanA.Length; i++) rspanA[i] *= rscaleA;
        WeightB = ArrayND.Zeros(Rank, FanOut);
    }

    /// <summary>
    ///     计算 LoRA 贡献：output = (alpha/r) * input × A × B
    /// </summary>
    private ArrayND ComputeLoraContribution(ArrayND input, AutogradContext? ctx = null)
    {
        var batch = input.Shape[0];

        var intermediate = MatMulForward(input, WeightA);

        var output = MatMulForward(intermediate, WeightB);

        var scale = ScaleFactor;
        var resultSpan = output.AsWriteSpan();
        for (var i = 0; i < resultSpan.Length; i++) resultSpan[i] *= scale;

        return output;
    }

    #region 矩阵乘法辅助

    private static ArrayND MatMulForward(ArrayND a, ArrayND b)
    {
        var rows = a.Shape[0];
        var inner = a.Shape[1];
        var cols = b.Shape[1];

        var result = ArrayND.Zeros(rows, cols);
        var spanResult = result.AsWriteSpan();
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();

        for (var i = 0; i < rows; i++)
        for (var j = 0; j < cols; j++)
        {
            var sum = 0.0f;
            for (var k = 0; k < inner; k++) sum += spanA[i * inner + k] * spanB[k * cols + j];
            spanResult[i * cols + j] = sum;
        }

        return result;
    }

    /// <summary>
    ///     逐元素加法
    /// </summary>
    private static ArrayND ElementWiseAdd(ArrayND a, ArrayND b)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanResult = result.AsWriteSpan();

        for (var i = 0; i < spanResult.Length; i++) spanResult[i] = spanA[i] + spanB[i];

        return result;
    }

    #endregion
}

/// <summary>
///     简单参数实现 —— 用于 LoRA 参数支持 IParameter 接口
/// </summary>
public sealed class SimpleParameter : IParameter
{
    /// <summary>
    ///     创建简单参数
    /// </summary>
    public SimpleParameter(ArrayND value)
    {
        Value = value;
    }

    /// <summary>参数值</summary>
    public ArrayND Value { get; }

    /// <summary>参数梯度</summary>
    public ArrayND? Grad => Value.Grad;
}
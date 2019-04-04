using Std.DL.Training;

namespace Std.DL.Flux.MoE;

/// <summary>
///     混合专家层 —— N 个专家 + Top-K 路由器
///     每个 token 被路由到 Top-K 个专家，输出为专家输出的加权组合。
///     每个专家是一个两层 FFN：Dense(dModel, dFFN) → ReLU → Dense(dFFN, dModel)
/// </summary>
public class MoELayer : ITrainableModel
{
    private readonly List<(Dense Up, Dense Down)> _experts;

    /// <summary>
    ///     创建混合专家层
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="dFFN">前馈维度</param>
    /// <param name="numExperts">专家数量</param>
    /// <param name="topK">每个 token 选择的专家数</param>
    public MoELayer(int dModel, int dFFN, int numExperts, int topK = 2)
    {
        DModel = dModel;
        DFFN = dFFN;
        NumExperts = numExperts;
        TopK = topK;
        Router = new ExpertRouter(dModel, numExperts, topK);
        _experts = new(numExperts);

        for (var i = 0; i < numExperts; i++) _experts.Add((new Dense(dModel, dFFN), new Dense(dFFN, dModel)));
    }

    /// <summary>专家路由器</summary>
    public ExpertRouter Router { get; }

    /// <summary>专家前馈网络列表（Up 投影 + Down 投影）</summary>
    public IReadOnlyList<(Dense Up, Dense Down)> Experts => _experts.AsReadOnly();

    /// <summary>最近一次前向传播的门控分数（用于计算负载均衡损失）</summary>
    public float[]? LastGateScores { get; private set; }

    /// <summary>模型维度</summary>
    public int DModel { get; }

    /// <summary>前馈维度</summary>
    public int DFFN { get; }

    /// <summary>专家数量</summary>
    public int NumExperts { get; }

    /// <summary>每个 token 选择的专家数</summary>
    public int TopK { get; }

    /// <summary>
    ///     前向传播（无自动微分）：稀疏计算，仅计算被选中的专家
    /// </summary>
    /// <param name="input">输入张量 [..., dModel]</param>
    /// <returns>输出张量 [..., dModel]</returns>
    public ArrayND forward(ArrayND input)
    {
        var originalShape = input.Shape;
        var ndim = input.Shape.Length;
        var numTokens = 1;
        for (var d = 0; d < ndim - 1; d++) numTokens *= input.Shape[d];

        var flatInput = input.Reshape(numTokens, DModel);
        var (topKIndices, topKWeights, gateScores) = Router.Route(flatInput);
        LastGateScores = gateScores;

        #region 稀疏专家计算

        var output = ArrayND.Zeros(numTokens, DModel);
        var spanOutput = output.AsWriteSpan();
        var spanInput = flatInput.AsSpan();

        for (var t = 0; t < numTokens; t++)
        for (var k = 0; k < TopK; k++)
        {
            var expertIdx = topKIndices[t * TopK + k];
            var weight = topKWeights[t * TopK + k];

            var tokenInput = ArrayND.Zeros(1, DModel);
            var spanToken = tokenInput.AsWriteSpan();
            for (var i = 0; i < DModel; i++) spanToken[i] = spanInput[t * DModel + i];

            var (upProj, downProj) = _experts[expertIdx];
            var hidden = upProj.forward(tokenInput);
            var activated = Activations.ReLUForward(hidden);
            var expertOut = downProj.forward(activated);

            var spanExpertOut = expertOut.AsSpan();
            for (var i = 0; i < DModel; i++) spanOutput[t * DModel + i] += weight * spanExpertOut[i];
        }

        #endregion

        var outShape = new int[ndim];
        Array.Copy(originalShape, outShape, ndim);
        outShape[ndim - 1] = DModel;
        return output.Reshape(outShape);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）：全量计算所有专家，用路由权重掩码加权组合
    /// </summary>
    /// <param name="input">输入张量 [..., dModel]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>输出张量 [..., dModel]</returns>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var originalShape = input.Shape;
        var ndim = input.Shape.Length;
        var numTokens = 1;
        for (var d = 0; d < ndim - 1; d++) numTokens *= input.Shape[d];

        var flatInput = input.ReshapeWithGrad([numTokens, DModel], ctx);
        var (topKIndices, topKWeights, gateScores) = Router.Route(flatInput, ctx);
        LastGateScores = gateScores;

        #region 全量专家计算 + 权重掩码组合

        var combined = ArrayND.Zeros(numTokens, DModel);

        for (var e = 0; e < NumExperts; e++)
        {
            var (upProj, downProj) = _experts[e];
            var hidden = upProj.forward(flatInput, ctx);
            var activated = Activations.ReLU(hidden, ctx);
            var expertOut = downProj.forward(activated, ctx);

            var weightMask = CreateWeightMask(e, topKIndices, topKWeights, numTokens);
            var weightedOut = ElementWiseMulWithGrad(expertOut, weightMask, ctx);
            combined = AddArraysWithGrad(combined, weightedOut, ctx);
        }

        #endregion

        var outShape = new int[ndim];
        Array.Copy(originalShape, outShape, ndim);
        outShape[ndim - 1] = DModel;
        return combined.ReshapeWithGrad(outShape, ctx);
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in Router.Parameters()) yield return p;

        foreach (var (up, down) in _experts)
        {
            foreach (var p in up.Parameters()) yield return p;

            foreach (var p in down.Parameters()) yield return p;
        }
    }

    /// <summary>
    ///     为指定专家创建路由权重掩码
    /// </summary>
    /// <param name="expertIdx">专家索引</param>
    /// <param name="topKIndices">Top-K 专家索引 [numTokens * topK]</param>
    /// <param name="topKWeights">Top-K 归一化权重 [numTokens * topK]</param>
    /// <param name="numTokens">token 数量</param>
    /// <returns>权重掩码 [numTokens, dModel]</returns>
    private ArrayND CreateWeightMask(int expertIdx, int[] topKIndices, float[] topKWeights, int numTokens)
    {
        var mask = ArrayND.Zeros(numTokens, DModel);
        var spanMask = mask.AsWriteSpan();

        for (var t = 0; t < numTokens; t++)
        for (var k = 0; k < TopK; k++)
            if (topKIndices[t * TopK + k] == expertIdx)
            {
                var w = topKWeights[t * TopK + k];
                var offset = t * DModel;
                for (var i = 0; i < DModel; i++) spanMask[offset + i] = w;

                break;
            }

        return mask;
    }

    private static ArrayND ElementWiseMulWithGrad(ArrayND a, ArrayND b, AutogradContext ctx)
    {
        var result = ElementWiseMul(a, b);

        ctx.Record(result, [a, b], outputGrads =>
        {
            var dResult = outputGrads[0];
            var spanDR = dResult.AsSpan();
            var spanA = a.AsSpan();
            var spanB = b.AsSpan();

            var dA = ArrayND.Zeros(a.Shape);
            var dB = ArrayND.Zeros(b.Shape);
            var spanDA = dA.AsWriteSpan();
            var spanDB = dB.AsWriteSpan();

            for (var i = 0; i < spanDR.Length; i++)
            {
                spanDA[i] = spanDR[i] * spanB[i];
                spanDB[i] = spanDR[i] * spanA[i];
            }

            return [dA, dB];
        });

        return result;
    }

    private static ArrayND ElementWiseMul(ArrayND a, ArrayND b)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();

        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] * spanB[i];

        return result;
    }

    private static ArrayND AddArraysWithGrad(ArrayND a, ArrayND b, AutogradContext ctx)
    {
        var sum = AddArrays(a, b);

        ctx.Record(sum, [a, b], outputGrads =>
        {
            var dSum = outputGrads[0];
            return [dSum, dSum];
        });

        return sum;
    }

    private static ArrayND AddArrays(ArrayND a, ArrayND b)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();

        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] + spanB[i];

        return result;
    }
}
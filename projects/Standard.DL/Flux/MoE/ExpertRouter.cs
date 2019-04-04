namespace Std.DL.Flux.MoE;

/// <summary>
///     专家路由器 —— Top-K 门控路由
///     为每个 token 计算门控分数，经 Softmax 归一化后选择 Top-K 个专家，
///     返回选中的专家索引、归一化权重和完整门控概率（用于负载均衡损失）
/// </summary>
public class ExpertRouter : ILayer
{
    /// <summary>
    ///     创建专家路由器
    /// </summary>
    /// <param name="dModel">模型维度</param>
    /// <param name="numExperts">专家数量</param>
    /// <param name="topK">每个 token 选择的专家数</param>
    public ExpertRouter(int dModel, int numExperts, int topK = 2)
    {
        if (topK > numExperts) throw new ArgumentException($"Top-K 值 {topK} 不能超过专家数量 {numExperts}");

        if (topK < 1) throw new ArgumentException($"Top-K 值不能小于 1，当前值：{topK}");

        Gate = new Dense(dModel, numExperts);
        NumExperts = numExperts;
        TopK = topK;
    }

    /// <summary>门控投影层 [dModel, numExperts]</summary>
    public Dense Gate { get; }

    /// <summary>专家数量</summary>
    public int NumExperts { get; }

    /// <summary>每个 token 选择的专家数</summary>
    public int TopK { get; }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return Gate.Parameters();
    }

    /// <summary>
    ///     前向路由：计算门控分数 → Softmax → 选择 Top-K 专家
    /// </summary>
    /// <param name="input">输入张量 [numTokens, dModel]</param>
    /// <returns>
    ///     topKIndices: [numTokens * topK] 每个 token 的 Top-K 专家索引
    ///     topKWeights: [numTokens * topK] 每个 token 的 Top-K 归一化权重
    ///     gateScores: [numTokens * numExperts] 门控 Softmax 概率（用于负载均衡损失）
    /// </returns>
    public (int[] topKIndices, float[] topKWeights, float[] gateScores) Route(ArrayND input)
    {
        var gateLogits = Gate.forward(input);
        var gateProbs = Activations.SoftmaxAxis(gateLogits, gateLogits.Shape.Length - 1);
        return SelectTopK(gateProbs);
    }

    /// <summary>
    ///     带自动微分的路由：门控投影和 Softmax 参与梯度计算
    /// </summary>
    /// <param name="input">输入张量 [numTokens, dModel]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>
    ///     topKIndices: [numTokens * topK] 每个 token 的 Top-K 专家索引
    ///     topKWeights: [numTokens * topK] 每个 token 的 Top-K 归一化权重
    ///     gateScores: [numTokens * numExperts] 门控 Softmax 概率（用于负载均衡损失）
    /// </returns>
    public (int[] topKIndices, float[] topKWeights, float[] gateScores) Route(ArrayND input, AutogradContext ctx)
    {
        var gateLogits = Gate.forward(input, ctx);
        var gateProbs = Activations.Softmax(gateLogits, ctx);
        return SelectTopK(gateProbs);
    }

    /// <summary>
    ///     从门控概率中选择 Top-K 专家并归一化权重
    /// </summary>
    /// <param name="gateProbs">门控概率 [numTokens, numExperts]</param>
    /// <returns>Top-K 索引、归一化权重和完整门控概率</returns>
    private (int[] topKIndices, float[] topKWeights, float[] gateScores) SelectTopK(ArrayND gateProbs)
    {
        var numTokens = gateProbs.Shape[0];
        var topKIndices = new int[numTokens * TopK];
        var topKWeights = new float[numTokens * TopK];
        var gateScores = new float[numTokens * NumExperts];

        var spanProbs = gateProbs.AsSpan();
        for (var i = 0; i < gateScores.Length; i++) gateScores[i] = spanProbs[i];

        for (var t = 0; t < numTokens; t++)
        {
            var offset = t * NumExperts;
            var experts = new (int idx, float score)[NumExperts];
            for (var e = 0; e < NumExperts; e++) experts[e] = (e, spanProbs[offset + e]);

            Array.Sort(experts, (a, b) => b.score.CompareTo(a.score));

            var weightSum = 0.0f;
            for (var k = 0; k < TopK; k++) weightSum += experts[k].score;

            for (var k = 0; k < TopK; k++)
            {
                topKIndices[t * TopK + k] = experts[k].idx;
                topKWeights[t * TopK + k] = weightSum > 0.0f ? experts[k].score / weightSum : 0.0f;
            }
        }

        return (topKIndices, topKWeights, gateScores);
    }
}
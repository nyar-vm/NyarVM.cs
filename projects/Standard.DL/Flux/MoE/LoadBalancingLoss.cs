namespace Std.DL.Flux.MoE;

/// <summary>
///     负载均衡损失 —— 防止路由坍缩的辅助损失
///     计算：coefficient * numExperts * Σ(f_i * P_i)
///     其中 f_i = 被路由到专家 i 的 token 比例，
///     P_i = 分配给专家 i 的路由概率比例。
///     当所有专家被均匀使用时，损失最小化。
/// </summary>
public static class LoadBalancingLoss
{
    /// <summary>
    ///     计算负载均衡辅助损失
    /// </summary>
    /// <param name="gateScores">门控 Softmax 概率 [numTokens * numExperts]</param>
    /// <param name="numExperts">专家数量</param>
    /// <param name="numTokens">token 数量</param>
    /// <param name="coefficient">损失系数（默认 0.01）</param>
    /// <returns>标量损失值</returns>
    public static float Compute(float[] gateScores, int numExperts, int numTokens, float coefficient = 0.01f)
    {
        if (gateScores.Length != numTokens * numExperts)
            throw new ArgumentException(
                $"门控分数长度 {gateScores.Length} 与 numTokens({numTokens}) * numExperts({numExperts}) = {numTokens * numExperts} 不匹配");

        if (numTokens == 0) return 0.0f;

        var f = new float[numExperts];
        var P = new float[numExperts];

        for (var t = 0; t < numTokens; t++)
        {
            var maxIdx = 0;
            var maxScore = gateScores[t * numExperts];

            for (var e = 1; e < numExperts; e++)
            {
                var score = gateScores[t * numExperts + e];
                if (score > maxScore)
                {
                    maxScore = score;
                    maxIdx = e;
                }
            }

            f[maxIdx] += 1.0f;

            for (var e = 0; e < numExperts; e++) P[e] += gateScores[t * numExperts + e];
        }

        var loss = 0.0f;
        for (var e = 0; e < numExperts; e++)
        {
            f[e] /= numTokens;
            P[e] /= numTokens;
            loss += f[e] * P[e];
        }

        return coefficient * numExperts * loss;
    }
}
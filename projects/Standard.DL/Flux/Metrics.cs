namespace Std.DL.Flux;

/// <summary>
///     模型评估指标 —— Perplexity / Accuracy / Top-k Accuracy
/// </summary>
public static class Metrics
{
    /// <summary>
    ///     计算困惑度（Perplexity = exp(cross_entropy_loss)）
    ///     PPL 越低越好，1.0 表示完美预测
    /// </summary>
    /// <param name="logits">模型输出 logits [batch, numClasses]</param>
    /// <param name="targets">目标类别索引 [batch, 1]</param>
    /// <returns>困惑度值</returns>
    public static float Perplexity(ArrayND logits, ArrayND targets)
    {
        var (ceLoss, _) = Losses.SoftmaxCrossEntropyForward(logits, targets);
        return MathF.Exp(ceLoss);
    }

    /// <summary>
    ///     计算分类准确率
    /// </summary>
    /// <param name="logits">模型输出 logits [batch, numClasses]</param>
    /// <param name="targets">目标类别索引 [batch, 1]</param>
    /// <returns>准确率 [0, 1]</returns>
    public static float Accuracy(ArrayND logits, ArrayND targets)
    {
        var batch = logits.Shape[0];
        var numClasses = logits.Shape[1];
        var spanLogits = logits.AsSpan();
        var spanTargets = targets.AsSpan();

        var correct = 0;
        for (var n = 0; n < batch; n++)
        {
            var offset = n * numClasses;
            var maxVal = float.NegativeInfinity;
            var maxIdx = 0;
            for (var i = 0; i < numClasses; i++)
                if (spanLogits[offset + i] > maxVal)
                {
                    maxVal = spanLogits[offset + i];
                    maxIdx = i;
                }

            if (maxIdx == (int)spanTargets[n]) correct++;
        }

        return (float)correct / batch;
    }

    /// <summary>
    ///     计算 Top-k 准确率（预测的 top-k 中包含正确答案的比例）
    /// </summary>
    /// <param name="logits">模型输出 logits [batch, numClasses]</param>
    /// <param name="targets">目标类别索引 [batch, 1]</param>
    /// <param name="k">Top-k 值</param>
    /// <returns>Top-k 准确率 [0, 1]</returns>
    public static float TopKAccuracy(ArrayND logits, ArrayND targets, int k)
    {
        var batch = logits.Shape[0];
        var numClasses = logits.Shape[1];
        var spanLogits = logits.AsSpan();
        var spanTargets = targets.AsSpan();

        var correct = 0;
        for (var n = 0; n < batch; n++)
        {
            var offset = n * numClasses;
            var values = new float[numClasses];
            var indices = new int[numClasses];
            for (var i = 0; i < numClasses; i++)
            {
                values[i] = spanLogits[offset + i];
                indices[i] = i;
            }

            Array.Sort(indices, (a, b) => values[b].CompareTo(values[a]));

            var target = (int)spanTargets[n];
            for (var i = 0; i < System.Math.Min(k, numClasses); i++)
                if (indices[i] == target)
                {
                    correct++;
                    break;
                }
        }

        return (float)correct / batch;
    }
}
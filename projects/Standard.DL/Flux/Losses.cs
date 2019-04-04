namespace Std.DL.Flux;

/// <summary>
///     损失函数
/// </summary>
public static class Losses
{
    /// <summary>
    ///     Softmax + 交叉熵损失前向
    /// </summary>
    /// <param name="logits">模型输出 [batch, numClasses]</param>
    /// <param name="labels">真实标签索引 [batch, 1]</param>
    /// <returns>(平均损失, softmax概率)</returns>
    public static (float Loss, ArrayND Probs) SoftmaxCrossEntropyForward(ArrayND logits, ArrayND labels)
    {
        var batch = logits.Shape[0];
        var classes = logits.Shape[1];
        var spanLogits = logits.AsSpan();
        var spanLabels = labels.AsSpan();

        var probs = ArrayND.Zeros(batch, classes);
        var spanProbs = probs.AsWriteSpan();

        var totalLoss = 0.0f;

        for (var i = 0; i < batch; i++)
        {
            var maxLogit = float.MinValue;
            for (var j = 0; j < classes; j++) maxLogit = MathF.Max(maxLogit, spanLogits[i * classes + j]);

            var sumExp = 0.0f;
            var exps = new float[classes];
            for (var j = 0; j < classes; j++)
            {
                exps[j] = MathF.Exp(spanLogits[i * classes + j] - maxLogit);
                sumExp += exps[j];
            }

            for (var j = 0; j < classes; j++) spanProbs[i * classes + j] = exps[j] / sumExp;

            var labelIdx = (int)spanLabels[i];
            totalLoss += -MathF.Log(MathF.Max(spanProbs[i * classes + labelIdx], 1e-8f));
        }

        return (totalLoss / batch, probs);
    }

    /// <summary>
    ///     Softmax 交叉熵反向梯度：dloss/dlogits = probs - one_hot(labels)
    /// </summary>
    public static ArrayND SoftmaxCrossEntropyBackward(ArrayND probs, ArrayND labels)
    {
        var batch = probs.Shape[0];
        var classes = probs.Shape[1];
        var grad = probs.Clone();
        var spanGrad = grad.AsWriteSpan();
        var spanLabels = labels.AsSpan();

        for (var i = 0; i < batch; i++)
        {
            var labelIdx = (int)spanLabels[i];
            spanGrad[i * classes + labelIdx] -= 1.0f;
        }

        for (var i = 0; i < spanGrad.Length; i++) spanGrad[i] /= batch;

        return grad;
    }

    /// <summary>
    ///     Softmax 交叉熵损失（带自动微分记录）
    /// </summary>
    /// <param name="logits">模型输出 [batch, numClasses]</param>
    /// <param name="labels">真实标签索引 [batch, 1]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>(损失标量张量, softmax概率)</returns>
    public static (ArrayND LossTensor, ArrayND Probs) SoftmaxCrossEntropy(ArrayND logits, ArrayND labels,
        AutogradContext ctx)
    {
        var (lossVal, probs) = SoftmaxCrossEntropyForward(logits, labels);

        var lossTensor = ArrayND.FromArray([lossVal], 1);

        ctx.Record(lossTensor, [logits], outputGrads =>
        {
            var dLogits = SoftmaxCrossEntropyBackward(probs, labels);
            return [dLogits];
        });

        return (lossTensor, probs);
    }

    /// <summary>
    ///     MSE 均方误差前向：loss = mean((predicted - target)^2)
    /// </summary>
    /// <param name="predicted">预测值</param>
    /// <param name="target">目标值</param>
    /// <returns>平均损失值</returns>
    public static float MSEForward(ArrayND predicted, ArrayND target)
    {
        var spanP = predicted.AsSpan();
        var spanT = target.AsSpan();
        var sum = 0.0f;
        for (var i = 0; i < spanP.Length; i++)
        {
            var diff = spanP[i] - spanT[i];
            sum += diff * diff;
        }

        return sum / spanP.Length;
    }

    /// <summary>
    ///     MSE 均方误差反向梯度：dLoss/dPredicted = 2 * (predicted - target) / N
    /// </summary>
    public static ArrayND MSEBackward(ArrayND predicted, ArrayND target)
    {
        var n = predicted.Size;
        var result = ArrayND.Zeros(predicted.Shape);
        var spanP = predicted.AsSpan();
        var spanT = target.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanP.Length; i++) spanR[i] = 2.0f * (spanP[i] - spanT[i]) / n;
        return result;
    }

    /// <summary>
    ///     MSE 均方误差（带自动微分记录）
    /// </summary>
    /// <param name="predicted">预测值</param>
    /// <param name="target">目标值</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>损失张量</returns>
    public static ArrayND MSE(ArrayND predicted, ArrayND target, AutogradContext ctx)
    {
        var lossVal = MSEForward(predicted, target);
        var lossTensor = ArrayND.FromArray([lossVal], 1);

        ctx.Record(lossTensor, [predicted], outputGrads =>
        {
            var dPredicted = MSEBackward(predicted, target);
            return [dPredicted];
        });

        return lossTensor;
    }

    /// <summary>
    ///     BCE 二元交叉熵前向：loss = -mean(target * log(predicted) + (1-target) * log(1-predicted))
    /// </summary>
    /// <param name="predicted">预测概率（需经 sigmoid）</param>
    /// <param name="target">真实标签 (0 或 1)</param>
    /// <returns>平均损失值</returns>
    public static float BCEForward(ArrayND predicted, ArrayND target)
    {
        var spanP = predicted.AsSpan();
        var spanT = target.AsSpan();
        var sum = 0.0f;
        var eps = 1e-8f;
        for (var i = 0; i < spanP.Length; i++)
        {
            var p = System.Math.Clamp(spanP[i], eps, 1.0f - eps);
            sum += spanT[i] * MathF.Log(p) + (1.0f - spanT[i]) * MathF.Log(1.0f - p);
        }

        return -sum / spanP.Length;
    }

    /// <summary>
    ///     BCE 二元交叉熵反向梯度：dLoss/dPredicted = (predicted - target) / (predicted * (1-predicted) * N)
    /// </summary>
    public static ArrayND BCEBackward(ArrayND predicted, ArrayND target)
    {
        var n = predicted.Size;
        var result = ArrayND.Zeros(predicted.Shape);
        var spanP = predicted.AsSpan();
        var spanT = target.AsSpan();
        var spanR = result.AsWriteSpan();
        var eps = 1e-8f;
        for (var i = 0; i < spanP.Length; i++)
        {
            var p = System.Math.Clamp(spanP[i], eps, 1.0f - eps);
            spanR[i] = (p - spanT[i]) / (p * (1.0f - p) * n);
        }

        return result;
    }

    /// <summary>
    ///     BCE 二元交叉熵（带自动微分记录）
    /// </summary>
    /// <param name="predicted">预测概率（需经 sigmoid）</param>
    /// <param name="target">真实标签 (0 或 1)</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>损失张量</returns>
    public static ArrayND BCE(ArrayND predicted, ArrayND target, AutogradContext ctx)
    {
        var lossVal = BCEForward(predicted, target);
        var lossTensor = ArrayND.FromArray([lossVal], 1);

        ctx.Record(lossTensor, [predicted], outputGrads =>
        {
            var dPredicted = BCEBackward(predicted, target);
            return [dPredicted];
        });

        return lossTensor;
    }

    /// <summary>
    ///     KL 散度前向：loss = sum(target * log(target / predicted))
    /// </summary>
    /// <param name="predicted">预测概率分布</param>
    /// <param name="target">目标概率分布</param>
    /// <returns>平均 KL 散度值</returns>
    public static float KLDivForward(ArrayND predicted, ArrayND target)
    {
        var spanP = predicted.AsSpan();
        var spanT = target.AsSpan();
        var sum = 0.0f;
        var eps = 1e-8f;
        var batch = predicted.Shape[0];
        for (var i = 0; i < spanP.Length; i++)
        {
            var t = spanT[i];
            if (t < eps) continue;

            var p = MathF.Max(spanP[i], eps);
            sum += t * MathF.Log(t / p);
        }

        return sum / batch;
    }

    /// <summary>
    ///     KL 散度反向梯度：dLoss/dPredicted = -target / (predicted * N)
    /// </summary>
    public static ArrayND KLDivBackward(ArrayND predicted, ArrayND target)
    {
        var batch = predicted.Shape[0];
        var result = ArrayND.Zeros(predicted.Shape);
        var spanP = predicted.AsSpan();
        var spanT = target.AsSpan();
        var spanR = result.AsWriteSpan();
        var eps = 1e-8f;
        for (var i = 0; i < spanP.Length; i++)
        {
            var t = spanT[i];
            if (t < eps)
                spanR[i] = 0.0f;
            else
                spanR[i] = -t / (MathF.Max(spanP[i], eps) * batch);
        }

        return result;
    }

    /// <summary>
    ///     KL 散度（带自动微分记录）
    /// </summary>
    /// <param name="predicted">预测概率分布</param>
    /// <param name="target">目标概率分布</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>损失张量</returns>
    public static ArrayND KLDiv(ArrayND predicted, ArrayND target, AutogradContext ctx)
    {
        var lossVal = KLDivForward(predicted, target);
        var lossTensor = ArrayND.FromArray([lossVal], 1);

        ctx.Record(lossTensor, [predicted], outputGrads =>
        {
            var dPredicted = KLDivBackward(predicted, target);
            return [dPredicted];
        });

        return lossTensor;
    }

    /// <summary>
    ///     Huber 损失前向：delta=1.0，|x|&lt;=delta 时为 0.5*x²，否则为 delta*(|x|-0.5*delta)
    /// </summary>
    /// <param name="predicted">预测值</param>
    /// <param name="target">目标值</param>
    /// <returns>平均 Huber 损失值</returns>
    public static float HuberForward(ArrayND predicted, ArrayND target)
    {
        const float delta = 1.0f;
        var spanP = predicted.AsSpan();
        var spanT = target.AsSpan();
        var sum = 0.0f;
        for (var i = 0; i < spanP.Length; i++)
        {
            var diff = spanP[i] - spanT[i];
            var absDiff = MathF.Abs(diff);
            if (absDiff <= delta)
                sum += 0.5f * diff * diff;
            else
                sum += delta * (absDiff - 0.5f * delta);
        }

        return sum / spanP.Length;
    }

    /// <summary>
    ///     Huber 损失反向梯度：|diff|&lt;=delta 时为 diff/N，否则为 delta*sign(diff)/N
    /// </summary>
    public static ArrayND HuberBackward(ArrayND predicted, ArrayND target)
    {
        const float delta = 1.0f;
        var n = predicted.Size;
        var result = ArrayND.Zeros(predicted.Shape);
        var spanP = predicted.AsSpan();
        var spanT = target.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanP.Length; i++)
        {
            var diff = spanP[i] - spanT[i];
            var absDiff = MathF.Abs(diff);
            if (absDiff <= delta)
                spanR[i] = diff / n;
            else
                spanR[i] = delta * MathF.Sign(diff) / n;
        }

        return result;
    }

    /// <summary>
    ///     Huber 损失（带自动微分记录）
    /// </summary>
    /// <param name="predicted">预测值</param>
    /// <param name="target">目标值</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>损失张量</returns>
    public static ArrayND Huber(ArrayND predicted, ArrayND target, AutogradContext ctx)
    {
        var lossVal = HuberForward(predicted, target);
        var lossTensor = ArrayND.FromArray([lossVal], 1);

        ctx.Record(lossTensor, [predicted], outputGrads =>
        {
            var dPredicted = HuberBackward(predicted, target);
            return [dPredicted];
        });

        return lossTensor;
    }

    /// <summary>
    ///     标签平滑交叉熵前向：将硬标签软化为 (1-smoothing)*one_hot + smoothing/numClasses
    ///     有效防止模型过度自信，提升泛化能力
    /// </summary>
    /// <param name="logits">预测 logits [batch, numClasses]</param>
    /// <param name="targets">目标类别索引 [batch, 1]</param>
    /// <param name="smoothing">平滑因子 (0 = 标准交叉熵, 0.1 = 常用值)</param>
    /// <returns>平均损失值</returns>
    public static float LabelSmoothingCrossEntropyForward(ArrayND logits, ArrayND targets, float smoothing = 0.1f)
    {
        var batch = logits.Shape[0];
        var numClasses = logits.Shape[1];
        var spanLogits = logits.AsSpan();
        var spanTargets = targets.AsSpan();

        var totalLoss = 0.0f;

        for (var n = 0; n < batch; n++)
        {
            var offset = n * numClasses;
            var targetClass = (int)spanTargets[n];

            var maxLogit = float.NegativeInfinity;
            for (var i = 0; i < numClasses; i++)
                if (spanLogits[offset + i] > maxLogit)
                    maxLogit = spanLogits[offset + i];

            var logSumExp = 0.0f;
            for (var i = 0; i < numClasses; i++) logSumExp += MathF.Exp(spanLogits[offset + i] - maxLogit);
            logSumExp = maxLogit + MathF.Log(logSumExp);

            var smoothTarget = smoothing / numClasses;
            var confidentTarget = 1.0f - smoothing + smoothTarget;

            var loss = 0.0f;
            for (var i = 0; i < numClasses; i++)
            {
                var logProb = spanLogits[offset + i] - logSumExp;
                var targetProb = i == targetClass ? confidentTarget : smoothTarget;
                loss -= targetProb * logProb;
            }

            totalLoss += loss;
        }

        return totalLoss / batch;
    }

    /// <summary>
    ///     标签平滑交叉熵（带自动微分）
    /// </summary>
    /// <param name="logits">预测 logits [batch, numClasses]</param>
    /// <param name="targets">目标类别索引 [batch, 1]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <param name="smoothing">平滑因子</param>
    /// <returns>损失张量</returns>
    public static ArrayND LabelSmoothingCrossEntropy(ArrayND logits, ArrayND targets, AutogradContext ctx,
        float smoothing = 0.1f)
    {
        var lossVal = LabelSmoothingCrossEntropyForward(logits, targets, smoothing);
        var lossTensor = ArrayND.FromArray([lossVal], 1);

        ctx.Record(lossTensor, [logits], outputGrads =>
        {
            var dLogits = LabelSmoothingCrossEntropyBackward(logits, targets, smoothing);
            return [dLogits];
        });

        return lossTensor;
    }

    private static ArrayND LabelSmoothingCrossEntropyBackward(ArrayND logits, ArrayND targets, float smoothing)
    {
        var batch = logits.Shape[0];
        var numClasses = logits.Shape[1];
        var dLogits = ArrayND.Zeros(logits.Shape);
        var spanLogits = logits.AsSpan();
        var spanTargets = targets.AsSpan();
        var spanD = dLogits.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var offset = n * numClasses;
            var targetClass = (int)spanTargets[n];

            var maxLogit = float.NegativeInfinity;
            for (var i = 0; i < numClasses; i++)
                if (spanLogits[offset + i] > maxLogit)
                    maxLogit = spanLogits[offset + i];

            var expSum = 0.0f;
            for (var i = 0; i < numClasses; i++) expSum += MathF.Exp(spanLogits[offset + i] - maxLogit);

            var smoothTarget = smoothing / numClasses;
            var confidentTarget = 1.0f - smoothing + smoothTarget;

            for (var i = 0; i < numClasses; i++)
            {
                var prob = MathF.Exp(spanLogits[offset + i] - maxLogit) / expSum;
                var targetProb = i == targetClass ? confidentTarget : smoothTarget;
                spanD[offset + i] = (prob - targetProb) / batch;
            }
        }

        return dLogits;
    }
}
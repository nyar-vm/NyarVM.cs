using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     梯度惩罚工具 —— WGAN-GP 风格梯度惩罚 + 梯度监控
///     GradientPenalty 用于 GAN 训练中强制判别器满足 Lipschitz 约束
///     GP = λ × E[(||∇_x D(x)||² - 1)²]
///     还提供梯度范数监控、梯度爆炸/消失检测等实用功能
/// </summary>
public static class GradientPenalty
{
    /// <summary>
    ///     计算 WGAN-GP 风格梯度惩罚
    ///     对插值样本的判别器输出求梯度，惩罚梯度范数偏离 1 的程度
    /// </summary>
    /// <param name="realData">真实样本 [batch, features...]</param>
    /// <param name="fakeData">生成样本 [batch, features...]</param>
    /// <param name="discriminatorFn">判别器前向函数</param>
    /// <param name="lambda">惩罚系数</param>
    /// <returns>梯度惩罚值</returns>
    public static float ComputeWGAN_GP(ArrayND realData, ArrayND fakeData,
        Func<ArrayND, ArrayND> discriminatorFn, float lambda = 10.0f)
    {
        var batch = realData.Shape[0];
        var epsilon = RandomEpsilon(batch);
        var interpolated = Interpolate(realData, fakeData, epsilon);

        var discOutput = discriminatorFn(interpolated);

        var gradNorm = EstimateGradientNorm(interpolated, discOutput);
        var penalty = lambda * (gradNorm - 1.0f) * (gradNorm - 1.0f);

        return penalty;
    }

    /// <summary>
    ///     计算模型参数的梯度范数
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="normType">范数类型（2 表示 L2 范数）</param>
    /// <returns>梯度范数</returns>
    public static float ParameterGradNorm(ITrainableModel model, float normType = 2.0f)
    {
        var totalNorm = 0.0f;

        foreach (var param in model.Parameters())
        {
            var grad = param.Grad;
            if (grad is null) continue;

            var spanG = grad.AsSpan();
            if (MathF.Abs(normType - 2.0f) < 0.01f)
                for (var i = 0; i < spanG.Length; i++)
                    totalNorm += spanG[i] * spanG[i];
            else if (MathF.Abs(normType - 1.0f) < 0.01f)
                for (var i = 0; i < spanG.Length; i++)
                    totalNorm += MathF.Abs(spanG[i]);
            else
                for (var i = 0; i < spanG.Length; i++)
                    totalNorm += MathF.Pow(MathF.Abs(spanG[i]), normType);
        }

        if (MathF.Abs(normType - 2.0f) < 0.01f) return MathF.Sqrt(totalNorm);

        if (MathF.Abs(normType - 1.0f) < 0.01f) return totalNorm;

        return MathF.Pow(totalNorm, 1.0f / normType);
    }

    /// <summary>
    ///     检测梯度异常（NaN / Infinity / 爆炸 / 消失）
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="explodeThreshold">梯度爆炸阈值</param>
    /// <param name="vanishThreshold">梯度消失阈值</param>
    /// <returns>梯度诊断结果</returns>
    public static GradientDiagnosis DiagnoseGradients(ITrainableModel model,
        float explodeThreshold = 1000.0f, float vanishThreshold = 1e-7f)
    {
        var hasNaN = false;
        var hasInf = false;
        var maxGrad = float.NegativeInfinity;
        var minGrad = float.PositiveInfinity;
        var totalNorm = 0.0f;
        var paramCount = 0;

        foreach (var param in model.Parameters())
        {
            var grad = param.Grad;
            if (grad is null) continue;

            var spanG = grad.AsSpan();
            for (var i = 0; i < spanG.Length; i++)
            {
                var g = spanG[i];
                if (float.IsNaN(g)) hasNaN = true;

                if (float.IsInfinity(g)) hasInf = true;

                if (!float.IsNaN(g) && !float.IsInfinity(g))
                {
                    if (g > maxGrad) maxGrad = g;

                    if (g < minGrad) minGrad = g;

                    totalNorm += g * g;
                }
            }

            paramCount++;
        }

        var gradNorm = MathF.Sqrt(totalNorm);

        return new GradientDiagnosis
        {
            HasNaN = hasNaN,
            HasInfinity = hasInf,
            IsExploding = gradNorm > explodeThreshold,
            IsVanishing = gradNorm < vanishThreshold,
            GradientNorm = gradNorm,
            MaxGradient = maxGrad,
            MinGradient = minGrad,
            ParameterCount = paramCount
        };
    }

    /// <summary>
    ///     计算逐层梯度范数（用于分析梯度流）
    /// </summary>
    /// <param name="model">模型</param>
    /// <returns>每层参数的梯度范数列表</returns>
    public static List<(string name, float norm)> LayerWiseGradNorm(ITrainableModel model)
    {
        var result = new List<(string, float)>();
        var idx = 0;

        foreach (var param in model.Parameters())
        {
            var grad = param.Grad;
            var norm = 0.0f;

            if (grad != null)
            {
                var spanG = grad.AsSpan();
                for (var i = 0; i < spanG.Length; i++)
                    norm += spanG[i] * spanG[i];
                norm = MathF.Sqrt(norm);
            }

            result.Add(($"param_{idx}", norm));
            idx++;
        }

        return result;
    }

    /// <summary>
    ///     对真实和生成样本进行插值
    /// </summary>
    private static ArrayND Interpolate(ArrayND real, ArrayND fake, ArrayND epsilon)
    {
        var result = ArrayND.Zeros(real.Shape);
        var spanR = real.AsSpan();
        var spanF = fake.AsSpan();
        var spanE = epsilon.AsSpan();
        var spanOut = result.AsWriteSpan();

        var featureSize = spanR.Length / real.Shape[0];
        for (var i = 0; i < spanR.Length; i++)
        {
            var b = i / featureSize;
            var eps = spanE[b];
            spanOut[i] = eps * spanR[i] + (1.0f - eps) * spanF[i];
        }

        return result;
    }

    /// <summary>
    ///     生成随机插值系数 [batch, 1]
    /// </summary>
    private static ArrayND RandomEpsilon(int batch)
    {
        var rng = new Random();
        var eps = ArrayND.Zeros(batch);
        var span = eps.AsWriteSpan();
        for (var i = 0; i < batch; i++)
            span[i] = (float)rng.NextDouble();
        return eps;
    }

    /// <summary>
    ///     估计梯度范数（有限差分法）
    /// </summary>
    private static float EstimateGradientNorm(ArrayND input, ArrayND output, float h = 1e-3f)
    {
        var spanIn = input.AsWriteSpan();
        var spanOut = output.AsSpan();
        var totalNorm = 0.0f;

        var batchSize = input.Shape[0];
        var featureSize = spanIn.Length / batchSize;

        for (var b = 0; b < batchSize; b++)
        {
            var gradNormSq = 0.0f;
            var outOff = b * (spanOut.Length / batchSize);

            for (var f = 0; f < System.Math.Min(featureSize, 64); f++)
            {
                var inOff = b * featureSize + f;
                var origVal = spanIn[inOff];

                spanIn[inOff] = origVal + h;
                var outPlus = output.AsSpan()[outOff];

                spanIn[inOff] = origVal - h;
                var outMinus = output.AsSpan()[outOff];

                spanIn[inOff] = origVal;

                var grad = (outPlus - outMinus) / (2.0f * h);
                gradNormSq += grad * grad;
            }

            totalNorm += gradNormSq;
        }

        return MathF.Sqrt(totalNorm / batchSize);
    }
}

/// <summary>
///     梯度诊断结果
/// </summary>
public struct GradientDiagnosis
{
    /// <summary>
    ///     是否包含 NaN
    /// </summary>
    public bool HasNaN;

    /// <summary>
    ///     是否包含 Infinity
    /// </summary>
    public bool HasInfinity;

    /// <summary>
    ///     是否梯度爆炸
    /// </summary>
    public bool IsExploding;

    /// <summary>
    ///     是否梯度消失
    /// </summary>
    public bool IsVanishing;

    /// <summary>
    ///     梯度总范数
    /// </summary>
    public float GradientNorm;

    /// <summary>
    ///     最大梯度值
    /// </summary>
    public float MaxGradient;

    /// <summary>
    ///     最小梯度值
    /// </summary>
    public float MinGradient;

    /// <summary>
    ///     参数数量
    /// </summary>
    public int ParameterCount;

    /// <summary>
    ///     生成诊断摘要
    /// </summary>
    /// <returns>摘要字符串</returns>
    public override string ToString()
    {
        var status = "正常";
        if (HasNaN)
            status = "NaN";
        else if (HasInfinity)
            status = "Infinity";
        else if (IsExploding)
            status = "爆炸";
        else if (IsVanishing) status = "消失";

        return
            $"梯度状态: {status}, 范数: {GradientNorm:F6}, 范围: [{MinGradient:F6}, {MaxGradient:F6}], 参数数: {ParameterCount}";
    }
}
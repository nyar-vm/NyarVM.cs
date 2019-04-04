namespace Std.DL.Flux;

/// <summary>
///     DDPM 噪声调度器 —— 管理扩散模型的前向加噪和反向去噪过程
///     支持线性噪声调度和余弦噪声调度
/// </summary>
public sealed class DDPMScheduler
{
    private readonly float[] _alphasCumprod;
    private readonly float[] _sqrtAlphasCumprod;
    private readonly float[] _sqrtOneMinusAlphasCumprod;

    /// <summary>
    ///     创建 DDPM 噪声调度器
    /// </summary>
    /// <param name="numTrainTimesteps">训练时间步数</param>
    /// <param name="betaStart">噪声起始值</param>
    /// <param name="betaEnd">噪声终止值</param>
    /// <param name="schedule">调度类型："linear" 或 "cosine"</param>
    public DDPMScheduler(int numTrainTimesteps = 1000, float betaStart = 0.0001f, float betaEnd = 0.02f,
        string schedule = "linear")
    {
        NumTrainTimesteps = numTrainTimesteps;

        var betas = schedule == "cosine"
            ? CosineBetas(numTrainTimesteps)
            : LinearBetas(numTrainTimesteps, betaStart, betaEnd);

        _alphasCumprod = new float[numTrainTimesteps];
        _sqrtAlphasCumprod = new float[numTrainTimesteps];
        _sqrtOneMinusAlphasCumprod = new float[numTrainTimesteps];

        var cumprod = 1.0f;
        for (var i = 0; i < numTrainTimesteps; i++)
        {
            cumprod *= 1.0f - betas[i];
            _alphasCumprod[i] = cumprod;
            _sqrtAlphasCumprod[i] = MathF.Sqrt(cumprod);
            _sqrtOneMinusAlphasCumprod[i] = MathF.Sqrt(1.0f - cumprod);
        }
    }

    /// <summary>
    ///     训练时间步数
    /// </summary>
    public int NumTrainTimesteps { get; }

    /// <summary>
    ///     累积 alpha 值
    /// </summary>
    public ReadOnlySpan<float> AlphasCumprod => _alphasCumprod;

    /// <summary>
    ///     前向加噪：x_t = sqrt(alpha_bar_t) * x_0 + sqrt(1 - alpha_bar_t) * noise
    /// </summary>
    /// <param name="x0">原始数据 [batch, ...]</param>
    /// <param name="noise">高斯噪声（与 x0 同形状）</param>
    /// <param name="timesteps">时间步索引 [batch, 1]，整数值 0..T-1</param>
    /// <returns>加噪后的数据</returns>
    public ArrayND AddNoise(ArrayND x0, ArrayND noise, ArrayND timesteps)
    {
        var batch = timesteps.Shape[0];
        var spanX0 = x0.AsSpan();
        var spanNoise = noise.AsSpan();
        var spanT = timesteps.AsSpan();

        var result = ArrayND.Zeros(x0.Shape);
        var spanR = result.AsWriteSpan();

        var featuresPerSample = x0.Size / batch;

        for (var n = 0; n < batch; n++)
        {
            var t = (int)spanT[n];
            var sqrtAlpha = _sqrtAlphasCumprod[t];
            var sqrtOneMinusAlpha = _sqrtOneMinusAlphasCumprod[t];

            var baseIdx = n * featuresPerSample;
            for (var i = 0; i < featuresPerSample; i++)
                spanR[baseIdx + i] = sqrtAlpha * spanX0[baseIdx + i] + sqrtOneMinusAlpha * spanNoise[baseIdx + i];
        }

        return result;
    }

    /// <summary>
    ///     计算损失目标：预测噪声（epsilon prediction）
    ///     目标就是输入的噪声本身
    /// </summary>
    /// <param name="noise">添加的噪声</param>
    /// <returns>损失目标（与 noise 相同）</returns>
    public ArrayND GetTarget(ArrayND noise)
    {
        return noise;
    }

    /// <summary>
    ///     DDPM 反向采样一步：从 x_t 预测 x_{t-1}
    ///     x_{t-1} = (1/sqrt(alpha_t)) * (x_t - beta_t/sqrt(1-alpha_bar_t) * predicted_noise) + sigma_t * z
    /// </summary>
    /// <param name="predictedNoise">模型预测的噪声</param>
    /// <param name="xT">当前时间步的样本</param>
    /// <param name="timestep">当前时间步索引</param>
    /// <param name="randomNoise">随机噪声（仅 t > 0 时使用）</param>
    /// <returns>去噪一步后的样本</returns>
    public ArrayND Step(ArrayND predictedNoise, ArrayND xT, int timestep, ArrayND? randomNoise = null)
    {
        var t = timestep;
        var alphaT = 1.0f - GetBeta(t);
        var alphaBarT = _alphasCumprod[t];
        var betaT = GetBeta(t);

        var sqrtRecipAlphaT = 1.0f / MathF.Sqrt(alphaT);
        var sqrtOneMinusAlphaBarT = MathF.Sqrt(1.0f - alphaBarT);

        var result = ArrayND.Zeros(xT.Shape);
        var spanPred = predictedNoise.AsSpan();
        var spanXT = xT.AsSpan();
        var spanR = result.AsWriteSpan();

        var sigmaT = t > 0 ? MathF.Sqrt(betaT) : 0.0f;

        for (var i = 0; i < spanR.Length; i++)
        {
            var mean = sqrtRecipAlphaT * (spanXT[i] - betaT / sqrtOneMinusAlphaBarT * spanPred[i]);
            spanR[i] = mean;
            if (t > 0 && randomNoise != null) spanR[i] += sigmaT * randomNoise.AsSpan()[i];
        }

        return result;
    }

    /// <summary>
    ///     获取指定时间步的 beta 值
    /// </summary>
    public float GetBeta(int timestep)
    {
        if (timestep == 0) return 1.0f - _alphasCumprod[0];

        return 1.0f - _alphasCumprod[timestep] / _alphasCumprod[timestep - 1];
    }

    /// <summary>
    ///     生成随机时间步（训练时采样用）
    /// </summary>
    /// <param name="batch">批次大小</param>
    /// <param name="seed">随机种子</param>
    /// <returns>时间步张量 [batch, 1]</returns>
    public ArrayND SampleTimesteps(int batch, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var timesteps = ArrayND.Zeros(batch, 1);
        var span = timesteps.AsWriteSpan();
        for (var i = 0; i < batch; i++) span[i] = rng.Next(0, NumTrainTimesteps);
        return timesteps;
    }

    /// <summary>
    ///     生成高斯噪声
    /// </summary>
    /// <param name="shape">噪声形状</param>
    /// <param name="seed">随机种子</param>
    /// <returns>高斯噪声张量</returns>
    public static ArrayND SampleNoise(int[] shape, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
        var size = shape.Aggregate(1, (a, b) => a * b);
        var data = new float[size];
        for (var i = 0; i < size; i++)
        {
            var u1 = 1.0f - rng.NextSingle();
            var u2 = 1.0f - rng.NextSingle();
            data[i] = MathF.Sqrt(-2.0f * MathF.Log(MathF.Max(u1, 1e-10f))) * MathF.Cos(2.0f * MathF.PI * u2);
        }

        return ArrayND.FromArray(data, shape);
    }

    private static float[] LinearBetas(int steps, float betaStart, float betaEnd)
    {
        var betas = new float[steps];
        for (var i = 0; i < steps; i++) betas[i] = betaStart + (betaEnd - betaStart) * i / (steps - 1);
        return betas;
    }

    private static float[] CosineBetas(int steps)
    {
        var betas = new float[steps];
        var s = 0.008f;
        for (var i = 0; i < steps; i++)
        {
            var t = (float)(i + 1) / steps;
            var fT = MathF.Cos((t + s) / (1 + s) * MathF.PI * 0.5f);
            var f0 = MathF.Cos(s / (1 + s) * MathF.PI * 0.5f);
            var alphaBar = fT * fT / (f0 * f0);
            var beta = 1.0f - alphaBar;
            if (i > 0) beta = 1.0f - alphaBar / (1.0f - betas.Take(i).Sum(b => 1.0f - b) / i * i / (i + 1));
            betas[i] = System.Math.Clamp(beta, 0.0f, 0.999f);
        }

        var cumprod = 1.0f;
        for (var i = 0; i < steps; i++)
        {
            var alphaBar = cumprod * (1.0f - betas[i]);
            if (alphaBar < 1e-4f) betas[i] = 1.0f - 1e-4f / cumprod;
            cumprod *= 1.0f - betas[i];
        }

        return betas;
    }
}
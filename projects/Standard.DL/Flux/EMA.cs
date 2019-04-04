using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     EMA（Exponential Moving Average）—— 指数移动平均模型权重平滑
///     训练过程中维护模型参数的平滑副本：ema_param = decay × ema_param + (1 - decay) × param
///     推理时使用 EMA 参数通常比原始参数效果更好
///     广泛用于 Diffusion Model、GAN、大模型训练
/// </summary>
public sealed class EMA
{
    private readonly Dictionary<ArrayND, ArrayND> _shadowParams = new();

    /// <summary>
    ///     创建 EMA
    /// </summary>
    /// <param name="decay">衰减率（0.999~0.9999），越大 EMA 参数变化越慢</param>
    public EMA(float decay = 0.999f)
    {
        if (decay is < 0.0f or > 1.0f) throw new ArgumentException($"衰减率必须在 [0, 1] 范围，当前={decay}");

        Decay = decay;
    }

    /// <summary>
    ///     衰减率
    /// </summary>
    public float Decay { get; }

    /// <summary>
    ///     更新次数
    /// </summary>
    public int UpdateCount { get; private set; }

    /// <summary>
    ///     注册模型参数，初始化 EMA 影子参数为当前参数的副本
    /// </summary>
    /// <param name="model">模型</param>
    public void Register(ITrainableModel model)
    {
        foreach (var param in model.Parameters())
        {
            var shadow = ArrayND.Zeros(param.Value.Shape);
            var spanSrc = param.Value.AsSpan();
            var spanDst = shadow.AsWriteSpan();
            for (var i = 0; i < spanSrc.Length; i++)
                spanDst[i] = spanSrc[i];
            _shadowParams[param.Value] = shadow;
        }
    }

    /// <summary>
    ///     更新 EMA 影子参数
    ///     shadow = decay × shadow + (1 - decay) × current
    /// </summary>
    /// <param name="model">模型</param>
    public void Update(ITrainableModel model)
    {
        UpdateCount++;
        foreach (var param in model.Parameters())
        {
            if (!_shadowParams.TryGetValue(param.Value, out var shadow))
            {
                shadow = ArrayND.Zeros(param.Value.Shape);
                var spanSrc = param.Value.AsSpan();
                var spanDst = shadow.AsWriteSpan();
                for (var i = 0; i < spanSrc.Length; i++)
                    spanDst[i] = spanSrc[i];
                _shadowParams[param.Value] = shadow;
            }

            var spanCurrent = param.Value.AsSpan();
            var spanShadow = shadow.AsWriteSpan();

            for (var i = 0; i < spanShadow.Length; i++)
                spanShadow[i] = Decay * spanShadow[i] + (1.0f - Decay) * spanCurrent[i];
        }
    }

    /// <summary>
    ///     将 EMA 影子参数复制到模型（用于推理/评估）
    ///     调用前应先 Backup 原始参数
    /// </summary>
    /// <param name="model">模型</param>
    public void ApplyShadow(ITrainableModel model)
    {
        foreach (var param in model.Parameters())
        {
            if (!_shadowParams.TryGetValue(param.Value, out var shadow)) continue;

            var spanCurrent = param.Value.AsWriteSpan();
            var spanShadow = shadow.AsSpan();
            for (var i = 0; i < spanCurrent.Length; i++)
                spanCurrent[i] = spanShadow[i];
        }
    }

    /// <summary>
    ///     备份模型当前参数（在 ApplyShadow 前调用）
    /// </summary>
    /// <param name="model">模型</param>
    /// <returns>参数备份字典</returns>
    public Dictionary<ArrayND, ArrayND> Backup(ITrainableModel model)
    {
        var backup = new Dictionary<ArrayND, ArrayND>();
        foreach (var param in model.Parameters())
        {
            var copy = ArrayND.Zeros(param.Value.Shape);
            var spanSrc = param.Value.AsSpan();
            var spanDst = copy.AsWriteSpan();
            for (var i = 0; i < spanSrc.Length; i++)
                spanDst[i] = spanSrc[i];
            backup[param.Value] = copy;
        }

        return backup;
    }

    /// <summary>
    ///     从备份恢复模型参数（在评估完成后调用）
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="backup">参数备份</param>
    public void Restore(ITrainableModel model, Dictionary<ArrayND, ArrayND> backup)
    {
        foreach (var param in model.Parameters())
        {
            if (!backup.TryGetValue(param.Value, out var saved)) continue;

            var spanCurrent = param.Value.AsWriteSpan();
            var spanSaved = saved.AsSpan();
            for (var i = 0; i < spanCurrent.Length; i++)
                spanCurrent[i] = spanSaved[i];
        }
    }

    /// <summary>
    ///     获取 EMA 影子参数与当前参数的平均绝对差
    /// </summary>
    /// <param name="model">模型</param>
    /// <returns>平均绝对差</returns>
    public float AverageDeviation(ITrainableModel model)
    {
        var totalDiff = 0.0f;
        var totalCount = 0;

        foreach (var param in model.Parameters())
        {
            if (!_shadowParams.TryGetValue(param.Value, out var shadow)) continue;

            var spanCurrent = param.Value.AsSpan();
            var spanShadow = shadow.AsSpan();

            for (var i = 0; i < spanShadow.Length; i++)
            {
                totalDiff += MathF.Abs(spanCurrent[i] - spanShadow[i]);
                totalCount++;
            }
        }

        return totalCount > 0 ? totalDiff / totalCount : 0.0f;
    }

    /// <summary>
    ///     使用动态衰减率更新（训练初期衰减率较小，逐渐增大）
    ///     decay = min(decay, (1 + updateCount) / (10 + updateCount))
    /// </summary>
    /// <param name="model">模型</param>
    public void UpdateWithDynamicDecay(ITrainableModel model)
    {
        UpdateCount++;
        var dynamicDecay = MathF.Min(Decay, (1.0f + UpdateCount) / (10.0f + UpdateCount));

        foreach (var param in model.Parameters())
        {
            if (!_shadowParams.TryGetValue(param.Value, out var shadow))
            {
                shadow = ArrayND.Zeros(param.Value.Shape);
                var spanSrc = param.Value.AsSpan();
                var spanDst = shadow.AsWriteSpan();
                for (var i = 0; i < spanSrc.Length; i++)
                    spanDst[i] = spanSrc[i];
                _shadowParams[param.Value] = shadow;
            }

            var spanCurrent = param.Value.AsSpan();
            var spanShadow = shadow.AsWriteSpan();

            for (var i = 0; i < spanShadow.Length; i++)
                spanShadow[i] = dynamicDecay * spanShadow[i] + (1.0f - dynamicDecay) * spanCurrent[i];
        }
    }
}
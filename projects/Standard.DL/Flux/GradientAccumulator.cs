namespace Std.DL.Flux;

/// <summary>
///     梯度累积器 —— 模拟大 batch 训练
///     在显存不足时，将大 batch 分成多个 micro-batch，
///     累积梯度后再执行一次 optimizer.Step()
///     用法：accum.Step() 替代 optimizer.Step()，accum.ZeroGrad() 替代 optimizer.ZeroGrad()
/// </summary>
public sealed class GradientAccumulator
{
    private readonly IOptimizer _optimizer;

    /// <summary>
    ///     创建梯度累积器
    /// </summary>
    /// <param name="optimizer">底层优化器</param>
    /// <param name="accumulationSteps">累积步数（等效 batch = micro_batch × accumulationSteps）</param>
    public GradientAccumulator(IOptimizer optimizer, int accumulationSteps)
    {
        _optimizer = optimizer;
        AccumulationSteps = accumulationSteps;
        CurrentStep = 0;
    }

    /// <summary>
    ///     当前累积步数
    /// </summary>
    public int CurrentStep { get; private set; }

    /// <summary>
    ///     是否达到累积阈值
    /// </summary>
    public bool IsReady => CurrentStep >= AccumulationSteps;

    /// <summary>
    ///     累积步数
    /// </summary>
    public int AccumulationSteps { get; }

    /// <summary>
    ///     累积梯度步 —— 每个微批次后调用
    ///     梯度会自动缩放（除以累积步数），达到阈值后执行 optimizer.Step()
    /// </summary>
    /// <param name="parameters">可训练参数</param>
    public void Step(IEnumerable<IParameter> parameters)
    {
        CurrentStep++;

        if (CurrentStep < AccumulationSteps) return;

        var paramList = parameters as IList<IParameter> ?? [.. parameters];
        foreach (var p in paramList)
        {
            if (p.Grad == null) continue;

            var span = p.Grad.AsWriteSpan();
            var scale = 1.0f / AccumulationSteps;
            for (var i = 0; i < span.Length; i++) span[i] *= scale;
        }

        _optimizer.Step(paramList);
        CurrentStep = 0;
    }

    /// <summary>
    ///     清零梯度 —— 仅在 optimizer.Step() 执行后调用
    ///     如果未达到累积阈值，不清零（继续累积）
    /// </summary>
    /// <param name="parameters">可训练参数</param>
    public void ZeroGrad(IEnumerable<IParameter> parameters)
    {
        if (CurrentStep != 0) return;

        _optimizer.ZeroGrad(parameters);
    }
}
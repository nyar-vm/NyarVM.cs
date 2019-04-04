namespace Std.DL.Flux;

/// <summary>
///     学习率调度器接口
/// </summary>
public interface ILRScheduler
{
    /// <summary>
    ///     获取当前步的学习率
    /// </summary>
    float LearningRate { get; }

    /// <summary>
    ///     前进一步（通常每个 epoch 或每个 batch 调用一次）
    /// </summary>
    void Step();

    /// <summary>
    ///     重置调度器状态
    /// </summary>
    void Reset();

    /// <summary>
    ///     设置学习率（用于 ReduceLROnPlateau 等动态调整）
    /// </summary>
    /// <param name="lr">新的学习率</param>
    void SetLearningRate(float lr)
    {
    }
}

/// <summary>
///     Cosine Annealing 学习率调度器
///     lr = eta_min + 0.5 * (eta_max - eta_min) * (1 + cos(pi * step / totalSteps))
/// </summary>
public sealed class CosineAnnealingLR : ILRScheduler
{
    private readonly float _etaMax;
    private readonly float _etaMin;
    private readonly int _totalSteps;
    private int _currentStep;

    /// <summary>
    ///     创建 Cosine Annealing 调度器
    /// </summary>
    /// <param name="optimizer">要调度的优化器</param>
    /// <param name="totalSteps">一个周期内的总步数</param>
    /// <param name="etaMin">最小学习率</param>
    public CosineAnnealingLR(IOptimizer optimizer, int totalSteps, float etaMin = 0.0f)
    {
        _etaMax = GetOptimizerLR(optimizer);
        _etaMin = etaMin;
        _totalSteps = totalSteps;
        _currentStep = 0;
    }

    /// <summary>
    ///     当前学习率
    /// </summary>
    public float LearningRate => GetLearningRate();

    /// <summary>
    ///     前进一步
    /// </summary>
    public void Step()
    {
        _currentStep = System.Math.Min(_currentStep + 1, _totalSteps);
    }

    /// <summary>
    ///     重置到第 0 步
    /// </summary>
    public void Reset()
    {
        _currentStep = 0;
    }

    /// <summary>
    ///     直接设置当前步数（用于热重启）
    /// </summary>
    /// <param name="step">目标步数</param>
    public void SetStep(int step)
    {
        _currentStep = System.Math.Clamp(step, 0, _totalSteps);
    }

    private float GetLearningRate()
    {
        if (_currentStep >= _totalSteps) return _etaMin;
        var progress = (float)_currentStep / _totalSteps;
        var cosine = MathF.Cos(MathF.PI * progress);
        return _etaMin + 0.5f * (_etaMax - _etaMin) * (1.0f + cosine);
    }

    private static float GetOptimizerLR(IOptimizer optimizer)
    {
        return optimizer switch
        {
            SGD sgd => 0.01f,
            Adam adam => 0.001f,
            AdamW adamw => 0.001f,
            RMSprop rms => 0.001f,
            _ => 0.01f
        };
    }
}

/// <summary>
///     StepLR 学习率调度器
///     每 stepSize 步将学习率乘以 gamma
/// </summary>
public sealed class StepLR : ILRScheduler
{
    private readonly float _gamma;
    private readonly float _initialLR;
    private readonly int _stepSize;
    private int _currentStep;

    /// <summary>
    ///     创建 StepLR 调度器
    /// </summary>
    /// <param name="initialLR">初始学习率</param>
    /// <param name="stepSize">衰减步长</param>
    /// <param name="gamma">衰减因子</param>
    public StepLR(float initialLR, int stepSize, float gamma = 0.1f)
    {
        _initialLR = initialLR;
        _stepSize = stepSize;
        _gamma = gamma;
        _currentStep = 0;
    }

    /// <summary>
    ///     当前学习率
    /// </summary>
    public float LearningRate => _initialLR * MathF.Pow(_gamma, _currentStep / _stepSize);

    /// <summary>
    ///     前进一步
    /// </summary>
    public void Step()
    {
        _currentStep++;
    }

    /// <summary>
    ///     重置到第 0 步
    /// </summary>
    public void Reset()
    {
        _currentStep = 0;
    }
}

/// <summary>
///     ReduceLROnPlateau 学习率调度器
///     当指标不再改善时衰减学习率
/// </summary>
public sealed class ReduceLROnPlateau : ILRScheduler
{
    private readonly float _factor;
    private readonly int _patience;
    private readonly float _threshold;
    private int _badEpochs;
    private float _bestMetric;
    private bool _initialized;

    /// <summary>
    ///     创建 ReduceLROnPlateau 调度器
    /// </summary>
    /// <param name="initialLR">初始学习率</param>
    /// <param name="factor">衰减因子</param>
    /// <param name="patience">容忍的不改善 epoch 数</param>
    /// <param name="threshold">判断改善的最小阈值</param>
    public ReduceLROnPlateau(float initialLR, float factor = 0.5f, int patience = 5, float threshold = 1e-4f)
    {
        LearningRate = initialLR;
        _factor = factor;
        _patience = patience;
        _threshold = threshold;
        _badEpochs = 0;
        _bestMetric = float.MaxValue;
        _initialized = false;
    }

    /// <summary>
    ///     当前学习率
    /// </summary>
    public float LearningRate { get; private set; }

    /// <summary>
    ///     前进一步（兼容接口，实际请使用 UpdateMetric）
    /// </summary>
    public void Step()
    {
    }

    /// <summary>
    ///     重置状态
    /// </summary>
    public void Reset()
    {
        _badEpochs = 0;
        _bestMetric = float.MaxValue;
        _initialized = false;
    }

    /// <summary>
    ///     根据当前指标值更新调度器（通常每个 epoch 调用一次）
    /// </summary>
    /// <param name="metric">当前指标值（如验证损失，值越小越好）</param>
    public void UpdateMetric(float metric)
    {
        if (!_initialized)
        {
            _bestMetric = metric;
            _initialized = true;
            _badEpochs = 0;
            return;
        }

        if (metric < _bestMetric - _threshold)
        {
            _bestMetric = metric;
            _badEpochs = 0;
        }
        else
        {
            _badEpochs++;
        }

        if (_badEpochs >= _patience)
        {
            LearningRate *= _factor;
            _badEpochs = 0;
        }
    }
}

/// <summary>
///     Linear Warmup 学习率调度器
///     前 warmupSteps 步线性从 0 增加到 peakLR，之后保持 peakLR
/// </summary>
public sealed class LinearWarmup : ILRScheduler
{
    private readonly float _peakLR;
    private readonly int _warmupSteps;
    private int _currentStep;

    /// <summary>
    ///     创建 Linear Warmup 调度器
    /// </summary>
    /// <param name="peakLR">预热后的目标学习率</param>
    /// <param name="warmupSteps">预热步数</param>
    public LinearWarmup(float peakLR, int warmupSteps)
    {
        _peakLR = peakLR;
        _warmupSteps = warmupSteps;
        _currentStep = 0;
    }

    /// <summary>
    ///     当前学习率
    /// </summary>
    public float LearningRate => _currentStep >= _warmupSteps
        ? _peakLR
        : _peakLR * _currentStep / _warmupSteps;

    /// <summary>
    ///     前进一步
    /// </summary>
    public void Step()
    {
        _currentStep++;
    }

    /// <summary>
    ///     重置到第 0 步
    /// </summary>
    public void Reset()
    {
        _currentStep = 0;
    }
}

/// <summary>
///     Cosine Decay with Warmup 学习率调度器
///     Transformer 训练标准配置：线性预热 + 余弦衰减
///     lr = warmup 阶段线性增长，之后 cosine 衰减到 etaMin
/// </summary>
public sealed class CosineDecayWithWarmup : ILRScheduler
{
    private readonly float _etaMin;
    private readonly float _peakLR;
    private readonly int _totalSteps;
    private readonly int _warmupSteps;
    private int _currentStep;

    /// <summary>
    ///     创建 Cosine Decay with Warmup 调度器
    /// </summary>
    /// <param name="peakLR">峰值学习率（warmup 结束时）</param>
    /// <param name="warmupSteps">预热步数</param>
    /// <param name="totalSteps">总训练步数</param>
    /// <param name="etaMin">最终最小学习率</param>
    public CosineDecayWithWarmup(float peakLR, int warmupSteps, int totalSteps, float etaMin = 0.0f)
    {
        _peakLR = peakLR;
        _warmupSteps = warmupSteps;
        _totalSteps = totalSteps;
        _etaMin = etaMin;
        _currentStep = 0;
    }

    /// <summary>
    ///     当前学习率
    /// </summary>
    public float LearningRate => GetLearningRate();

    /// <summary>
    ///     前进一步
    /// </summary>
    public void Step()
    {
        _currentStep = System.Math.Min(_currentStep + 1, _totalSteps);
    }

    /// <summary>
    ///     重置到第 0 步
    /// </summary>
    public void Reset()
    {
        _currentStep = 0;
    }

    private float GetLearningRate()
    {
        if (_currentStep < _warmupSteps) return _peakLR * _currentStep / _warmupSteps;

        if (_currentStep >= _totalSteps) return _etaMin;

        var progress = (float)(_currentStep - _warmupSteps) / (_totalSteps - _warmupSteps);
        var cosine = MathF.Cos(MathF.PI * progress);
        return _etaMin + 0.5f * (_peakLR - _etaMin) * (1.0f + cosine);
    }
}

/// <summary>
///     Cosine Annealing with Warm Restarts 学习率调度器（SGDR）
///     周期性余弦退火，每个周期结束后学习率重置到最大值
///     lr = eta_min + 0.5 * (eta_max - eta_min) * (1 + cos(pi * (step % T_0) / T_0))
///     每次 restart 后周期长度乘以 T_mult
/// </summary>
public sealed class CosineAnnealingWarmRestarts : ILRScheduler
{
    private readonly float _etaMax;
    private readonly float _etaMin;
    private readonly int _t0;
    private readonly float _tMult;
    private int _currentCycleLength;
    private int _currentStep;
    private int _cycleStart;

    /// <summary>
    ///     创建 Cosine Annealing with Warm Restarts 调度器
    /// </summary>
    /// <param name="initialLR">初始（最大）学习率</param>
    /// <param name="t0">第一个周期的步数</param>
    /// <param name="tMult">周期长度倍增因子</param>
    /// <param name="etaMin">最小学习率</param>
    public CosineAnnealingWarmRestarts(float initialLR, int t0, float tMult = 1.0f, float etaMin = 0.0f)
    {
        _etaMax = initialLR;
        _t0 = t0;
        _tMult = tMult;
        _etaMin = etaMin;
        _currentStep = 0;
        _cycleStart = 0;
        _currentCycleLength = t0;
    }

    /// <summary>
    ///     当前学习率
    /// </summary>
    public float LearningRate => GetLearningRate();

    /// <summary>
    ///     前进一步
    /// </summary>
    public void Step()
    {
        _currentStep++;
        var stepInCycle = _currentStep - _cycleStart;
        if (stepInCycle >= _currentCycleLength)
        {
            _cycleStart = _currentStep;
            _currentCycleLength = (int)(_currentCycleLength * _tMult);
            if (_currentCycleLength <= 0) _currentCycleLength = _t0;
        }
    }

    /// <summary>
    ///     重置到第 0 步
    /// </summary>
    public void Reset()
    {
        _currentStep = 0;
        _cycleStart = 0;
        _currentCycleLength = _t0;
    }

    private float GetLearningRate()
    {
        var stepInCycle = _currentStep - _cycleStart;
        var progress = (float)stepInCycle / _currentCycleLength;
        var cosine = MathF.Cos(MathF.PI * progress);
        return _etaMin + 0.5f * (_etaMax - _etaMin) * (1.0f + cosine);
    }
}

/// <summary>
///     OneCycleLR 学习率调度器
///     Super-Convergence 方法：先线性增到 max_lr，再余弦退火到 final_lr
///     一轮训练中同时完成 warmup 和 decay
/// </summary>
public sealed class OneCycleLR : ILRScheduler
{
    private readonly float _finalLR;
    private readonly float _maxLR;
    private readonly float _pctStart;
    private readonly int _totalSteps;
    private int _currentStep;

    /// <summary>
    ///     创建 OneCycleLR 调度器
    /// </summary>
    /// <param name="maxLR">峰值学习率</param>
    /// <param name="totalSteps">总训练步数</param>
    /// <param name="pctStart">warmup 占比（默认 0.3）</param>
    /// <param name="finalLR">最终学习率</param>
    public OneCycleLR(float maxLR, int totalSteps, float pctStart = 0.3f, float finalLR = 0.0001f)
    {
        _maxLR = maxLR;
        _totalSteps = totalSteps;
        _pctStart = pctStart;
        _finalLR = finalLR;
        _currentStep = 0;
    }

    /// <summary>
    ///     当前学习率
    /// </summary>
    public float LearningRate => GetLearningRate();

    /// <summary>
    ///     前进一步
    /// </summary>
    public void Step()
    {
        _currentStep = System.Math.Min(_currentStep + 1, _totalSteps);
    }

    /// <summary>
    ///     重置
    /// </summary>
    public void Reset()
    {
        _currentStep = 0;
    }

    private float GetLearningRate()
    {
        if (_currentStep >= _totalSteps) return _finalLR;

        var pct = (float)_currentStep / _totalSteps;
        if (pct < _pctStart)
        {
            var warmupProgress = pct / _pctStart;
            return _finalLR + (_maxLR - _finalLR) * warmupProgress;
        }

        var decayProgress = (pct - _pctStart) / (1.0f - _pctStart);
        var cosine = MathF.Cos(MathF.PI * decayProgress);
        return _finalLR + 0.5f * (_maxLR - _finalLR) * (1.0f + cosine);
    }
}
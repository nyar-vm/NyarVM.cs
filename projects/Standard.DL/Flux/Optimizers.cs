namespace Std.DL.Flux;

/// <summary>
///     优化器接口
/// </summary>
public interface IOptimizer
{
    /// <summary>
    ///     对一组参数执行一步优化
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    void Step(IEnumerable<IParameter> parameters);

    /// <summary>
    ///     清零所有参数的梯度
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    void ZeroGrad(IEnumerable<IParameter> parameters);
}

/// <summary>
///     SGD 随机梯度下降优化器
/// </summary>
public sealed class SGD : IOptimizer
{
    private readonly float _learningRate;
    private readonly float _momentum;
    private readonly Dictionary<ArrayND, ArrayND> _velocity = new();

    /// <summary>
    ///     创建 SGD 优化器
    /// </summary>
    /// <param name="learningRate">学习率</param>
    /// <param name="momentum">动量系数</param>
    public SGD(float learningRate, float momentum = 0.0f)
    {
        _learningRate = learningRate;
        _momentum = momentum;
    }

    /// <summary>
    ///     执行一步 SGD 更新
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void Step(IEnumerable<IParameter> parameters)
    {
        foreach (var param in parameters)
        {
            var value = param.Value;
            var grad = param.Grad;
            if (grad is null) continue;

            var spanV = value.AsWriteSpan();
            var spanG = grad.AsSpan();

            if (_momentum > 0.0f)
            {
                if (!_velocity.TryGetValue(value, out var velocity))
                {
                    velocity = ArrayND.Zeros(value.Shape);
                    _velocity[value] = velocity;
                }

                var spanVel = velocity.AsWriteSpan();
                for (var i = 0; i < spanV.Length; i++)
                {
                    spanVel[i] = _momentum * spanVel[i] + spanG[i];
                    spanV[i] -= _learningRate * spanVel[i];
                }
            }
            else
            {
                for (var i = 0; i < spanV.Length; i++) spanV[i] -= _learningRate * spanG[i];
            }
        }
    }

    /// <summary>
    ///     清零所有参数的梯度
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void ZeroGrad(IEnumerable<IParameter> parameters)
    {
        foreach (var param in parameters) param.Value.ZeroGrad();
    }
}

/// <summary>
///     AdamW 优化器（Adam with Decoupled Weight Decay）
/// </summary>
public sealed class AdamW : IOptimizer
{
    private readonly float _beta1;
    private readonly float _beta2;
    private readonly float _epsilon;
    private readonly float _learningRate;
    private readonly Dictionary<ArrayND, ArrayND> _m = new();
    private readonly Dictionary<ArrayND, ArrayND> _v = new();
    private readonly float _weightDecay;
    private int _stepCount;

    /// <summary>
    ///     创建 AdamW 优化器
    /// </summary>
    /// <param name="learningRate">学习率</param>
    /// <param name="weightDecay">权重衰减系数</param>
    /// <param name="beta1">一阶矩衰减率</param>
    /// <param name="beta2">二阶矩衰减率</param>
    /// <param name="epsilon">数值稳定项</param>
    public AdamW(float learningRate = 0.001f, float weightDecay = 0.01f, float beta1 = 0.9f, float beta2 = 0.999f,
        float epsilon = 1e-8f)
    {
        _learningRate = learningRate;
        _weightDecay = weightDecay;
        _beta1 = beta1;
        _beta2 = beta2;
        _epsilon = epsilon;
    }

    /// <summary>
    ///     执行一步 AdamW 更新
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void Step(IEnumerable<IParameter> parameters)
    {
        _stepCount++;
        var biasCorrection1 = 1.0f - MathF.Pow(_beta1, _stepCount);
        var biasCorrection2 = 1.0f - MathF.Pow(_beta2, _stepCount);

        foreach (var param in parameters)
        {
            var value = param.Value;
            var grad = param.Grad;
            if (grad is null) continue;

            if (!_m.TryGetValue(value, out var m))
            {
                m = ArrayND.Zeros(value.Shape);
                _m[value] = m;
            }

            if (!_v.TryGetValue(value, out var v))
            {
                v = ArrayND.Zeros(value.Shape);
                _v[value] = v;
            }

            var spanV = value.AsWriteSpan();
            var spanG = grad.AsSpan();
            var spanM = m.AsWriteSpan();
            var spanVt = v.AsWriteSpan();

            for (var i = 0; i < spanV.Length; i++)
            {
                spanV[i] -= _learningRate * _weightDecay * spanV[i];

                spanM[i] = _beta1 * spanM[i] + (1.0f - _beta1) * spanG[i];
                spanVt[i] = _beta2 * spanVt[i] + (1.0f - _beta2) * spanG[i] * spanG[i];

                var mHat = spanM[i] / biasCorrection1;
                var vHat = spanVt[i] / biasCorrection2;

                spanV[i] -= _learningRate * mHat / (MathF.Sqrt(vHat) + _epsilon);
            }
        }
    }

    /// <summary>
    ///     清零所有参数的梯度
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void ZeroGrad(IEnumerable<IParameter> parameters)
    {
        foreach (var param in parameters) param.Value.ZeroGrad();
    }
}

/// <summary>
///     RMSprop 优化器（均方根传播）
/// </summary>
public sealed class RMSprop : IOptimizer
{
    private readonly Dictionary<ArrayND, ArrayND> _cache = new();
    private readonly float _decayRate;
    private readonly float _epsilon;
    private readonly float _learningRate;

    /// <summary>
    ///     创建 RMSprop 优化器
    /// </summary>
    /// <param name="learningRate">学习率</param>
    /// <param name="decayRate">均方根衰减率</param>
    /// <param name="epsilon">数值稳定项</param>
    public RMSprop(float learningRate = 0.001f, float decayRate = 0.99f, float epsilon = 1e-8f)
    {
        _learningRate = learningRate;
        _decayRate = decayRate;
        _epsilon = epsilon;
    }

    /// <summary>
    ///     执行一步 RMSprop 更新
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void Step(IEnumerable<IParameter> parameters)
    {
        foreach (var param in parameters)
        {
            var value = param.Value;
            var grad = param.Grad;
            if (grad is null) continue;

            if (!_cache.TryGetValue(value, out var cache))
            {
                cache = ArrayND.Zeros(value.Shape);
                _cache[value] = cache;
            }

            var spanV = value.AsWriteSpan();
            var spanG = grad.AsSpan();
            var spanCache = cache.AsWriteSpan();

            for (var i = 0; i < spanV.Length; i++)
            {
                spanCache[i] = _decayRate * spanCache[i] + (1.0f - _decayRate) * spanG[i] * spanG[i];
                spanV[i] -= _learningRate * spanG[i] / (MathF.Sqrt(spanCache[i]) + _epsilon);
            }
        }
    }

    /// <summary>
    ///     清零所有参数的梯度
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void ZeroGrad(IEnumerable<IParameter> parameters)
    {
        foreach (var param in parameters) param.Value.ZeroGrad();
    }
}

/// <summary>
///     Adam 优化器
/// </summary>
public sealed class Adam : IOptimizer
{
    private readonly float _beta1;
    private readonly float _beta2;
    private readonly float _epsilon;
    private readonly float _learningRate;
    private readonly Dictionary<ArrayND, ArrayND> _m = new();
    private readonly Dictionary<ArrayND, ArrayND> _v = new();
    private int _stepCount;

    /// <summary>
    ///     创建 Adam 优化器
    /// </summary>
    /// <param name="learningRate">学习率</param>
    /// <param name="beta1">一阶矩衰减率</param>
    /// <param name="beta2">二阶矩衰减率</param>
    /// <param name="epsilon">数值稳定项</param>
    public Adam(float learningRate = 0.001f, float beta1 = 0.9f, float beta2 = 0.999f, float epsilon = 1e-8f)
    {
        _learningRate = learningRate;
        _beta1 = beta1;
        _beta2 = beta2;
        _epsilon = epsilon;
    }

    /// <summary>
    ///     执行一步 Adam 更新
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void Step(IEnumerable<IParameter> parameters)
    {
        _stepCount++;
        var biasCorrection1 = 1.0f - MathF.Pow(_beta1, _stepCount);
        var biasCorrection2 = 1.0f - MathF.Pow(_beta2, _stepCount);

        foreach (var param in parameters)
        {
            var value = param.Value;
            var grad = param.Grad;
            if (grad is null) continue;

            if (!_m.TryGetValue(value, out var m))
            {
                m = ArrayND.Zeros(value.Shape);
                _m[value] = m;
            }

            if (!_v.TryGetValue(value, out var v))
            {
                v = ArrayND.Zeros(value.Shape);
                _v[value] = v;
            }

            var spanV = value.AsWriteSpan();
            var spanG = grad.AsSpan();
            var spanM = m.AsWriteSpan();
            var spanVt = v.AsWriteSpan();

            for (var i = 0; i < spanV.Length; i++)
            {
                spanM[i] = _beta1 * spanM[i] + (1.0f - _beta1) * spanG[i];
                spanVt[i] = _beta2 * spanVt[i] + (1.0f - _beta2) * spanG[i] * spanG[i];

                var mHat = spanM[i] / biasCorrection1;
                var vHat = spanVt[i] / biasCorrection2;

                spanV[i] -= _learningRate * mHat / (MathF.Sqrt(vHat) + _epsilon);
            }
        }
    }

    /// <summary>
    ///     清零所有参数的梯度
    /// </summary>
    /// <param name="parameters">可训练参数列表</param>
    public void ZeroGrad(IEnumerable<IParameter> parameters)
    {
        foreach (var param in parameters) param.Value.ZeroGrad();
    }
}
using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     集中式权重初始化工具 —— 为模型参数提供标准化的初始化策略
///     支持 Xavier (Glorot)、He (Kaiming)、Orthogonal、Uniform、Normal 等初始化方法
///     自动根据层的 fan_in / fan_out 选择合适的初始化
/// </summary>
public static class WeightInitialization
{
    /// <summary>
    ///     对模型的所有参数应用指定初始化策略
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="strategy">初始化策略</param>
    /// <param name="seed">随机种子（可选）</param>
    public static void Initialize(ITrainableModel model, InitStrategy strategy, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();

        foreach (var param in model.Parameters())
        {
            var shape = param.Value.Shape;
            if (shape.Length < 2) continue;

            var fanIn = shape[0];
            var fanOut = shape[1];

            ApplyStrategy(param.Value, strategy, fanIn, fanOut, rng);
        }
    }

    /// <summary>
    ///     对单个参数应用初始化
    /// </summary>
    /// <param name="param">参数张量</param>
    /// <param name="strategy">初始化策略</param>
    /// <param name="seed">随机种子</param>
    public static void InitializeParam(ArrayND param, InitStrategy strategy, int? seed = null)
    {
        var rng = seed.HasValue ? new Random(seed.Value) : new Random();
        var shape = param.Shape;
        var fanIn = shape.Length >= 2 ? shape[0] : shape[0];
        var fanOut = shape.Length >= 2 ? shape[1] : 1;
        ApplyStrategy(param, strategy, fanIn, fanOut, rng);
    }

    /// <summary>
    ///     Xavier Uniform 初始化（Glorot Uniform）
    ///     适用于 tanh / sigmoid 激活函数
    ///     范围：[-sqrt(6 / (fan_in + fan_out)), sqrt(6 / (fan_in + fan_out))]
    /// </summary>
    /// <param name="param">参数张量</param>
    public static void XavierUniform(ArrayND param)
    {
        var shape = param.Shape;
        var fanIn = shape[0];
        var fanOut = shape.Length >= 2 ? shape[1] : 1;
        var limit = MathF.Sqrt(6.0f / (fanIn + fanOut));

        var rng = new Random();
        var span = param.AsWriteSpan();
        for (var i = 0; i < span.Length; i++)
            span[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * limit;
    }

    /// <summary>
    ///     Xavier Normal 初始化（Glorot Normal）
    ///     适用于 tanh / sigmoid 激活函数
    ///     标准差：sqrt(2 / (fan_in + fan_out))
    /// </summary>
    /// <param name="param">参数张量</param>
    public static void XavierNormal(ArrayND param)
    {
        var shape = param.Shape;
        var fanIn = shape[0];
        var fanOut = shape.Length >= 2 ? shape[1] : 1;
        var std = MathF.Sqrt(2.0f / (fanIn + fanOut));

        var rng = new Random();
        var span = param.AsWriteSpan();
        for (var i = 0; i < span.Length; i++)
            span[i] = NormalRandom(rng) * std;
    }

    /// <summary>
    ///     He Uniform 初始化（Kaiming Uniform）
    ///     适用于 ReLU / LeakyReLU 激活函数
    ///     范围：[-sqrt(6 / fan_in), sqrt(6 / fan_in)]
    /// </summary>
    /// <param name="param">参数张量</param>
    public static void HeUniform(ArrayND param)
    {
        var shape = param.Shape;
        var fanIn = shape[0];
        var limit = MathF.Sqrt(6.0f / fanIn);

        var rng = new Random();
        var span = param.AsWriteSpan();
        for (var i = 0; i < span.Length; i++)
            span[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * limit;
    }

    /// <summary>
    ///     He Normal 初始化（Kaiming Normal）
    ///     适用于 ReLU / LeakyReLU 激活函数
    ///     标准差：sqrt(2 / fan_in)
    /// </summary>
    /// <param name="param">参数张量</param>
    public static void HeNormal(ArrayND param)
    {
        var shape = param.Shape;
        var fanIn = shape[0];
        var std = MathF.Sqrt(2.0f / fanIn);

        var rng = new Random();
        var span = param.AsWriteSpan();
        for (var i = 0; i < span.Length; i++)
            span[i] = NormalRandom(rng) * std;
    }

    /// <summary>
    ///     将参数初始化为常数
    /// </summary>
    /// <param name="param">参数张量</param>
    /// <param name="value">常数值</param>
    public static void Constant(ArrayND param, float value)
    {
        var span = param.AsWriteSpan();
        for (var i = 0; i < span.Length; i++)
            span[i] = value;
    }

    /// <summary>
    ///     将偏置参数初始化为零
    /// </summary>
    /// <param name="model">模型</param>
    public static void ZeroBiases(ITrainableModel model)
    {
        foreach (var param in model.Parameters())
        {
            var shape = param.Value.Shape;
            if (shape.Length == 1)
            {
                var span = param.Value.AsWriteSpan();
                for (var i = 0; i < span.Length; i++)
                    span[i] = 0.0f;
            }
        }
    }

    /// <summary>
    ///     计算参数的 fan_in 和 fan_out
    /// </summary>
    /// <param name="shape">参数形状</param>
    /// <returns>(fan_in, fan_out)</returns>
    public static (int fanIn, int fanOut) ComputeFanInOut(int[] shape)
    {
        if (shape.Length < 2) return (shape[0], 1);

        var fanIn = shape[0];
        var fanOut = shape[1];

        for (var i = 2; i < shape.Length; i++)
        {
            fanIn *= shape[i];
            fanOut *= shape[i];
        }

        return (fanIn, fanOut);
    }

    private static void ApplyStrategy(ArrayND param, InitStrategy strategy, int fanIn, int fanOut, Random rng)
    {
        var span = param.AsWriteSpan();

        switch (strategy)
        {
            case InitStrategy.XavierUniform:
            {
                var limit = MathF.Sqrt(6.0f / (fanIn + fanOut));
                for (var i = 0; i < span.Length; i++)
                    span[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * limit;
                break;
            }
            case InitStrategy.XavierNormal:
            {
                var std = MathF.Sqrt(2.0f / (fanIn + fanOut));
                for (var i = 0; i < span.Length; i++)
                    span[i] = NormalRandom(rng) * std;
                break;
            }
            case InitStrategy.HeUniform:
            {
                var limit = MathF.Sqrt(6.0f / fanIn);
                for (var i = 0; i < span.Length; i++)
                    span[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * limit;
                break;
            }
            case InitStrategy.HeNormal:
            {
                var std = MathF.Sqrt(2.0f / fanIn);
                for (var i = 0; i < span.Length; i++)
                    span[i] = NormalRandom(rng) * std;
                break;
            }
            case InitStrategy.Normal:
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = NormalRandom(rng) * 0.02f;
                break;
            }
            case InitStrategy.Uniform:
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.02f;
                break;
            }
            case InitStrategy.Zeros:
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = 0.0f;
                break;
            }
            case InitStrategy.Ones:
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = 1.0f;
                break;
            }
        }
    }

    private static float NormalRandom(Random rng)
    {
        var u1 = 1.0 - rng.NextDouble();
        var u2 = 1.0 - rng.NextDouble();
        return MathF.Sqrt(-2.0f * MathF.Log((float)u1)) * MathF.Cos(2.0f * MathF.PI * (float)u2);
    }
}

/// <summary>
///     初始化策略枚举
/// </summary>
public enum InitStrategy
{
    /// <summary>
    ///     Xavier Uniform（Glorot Uniform）
    /// </summary>
    XavierUniform,

    /// <summary>
    ///     Xavier Normal（Glorot Normal）
    /// </summary>
    XavierNormal,

    /// <summary>
    ///     He Uniform（Kaiming Uniform）
    /// </summary>
    HeUniform,

    /// <summary>
    ///     He Normal（Kaiming Normal）
    /// </summary>
    HeNormal,

    /// <summary>
    ///     正态分布（std=0.02）
    /// </summary>
    Normal,

    /// <summary>
    ///     均匀分布（range=±0.02）
    /// </summary>
    Uniform,

    /// <summary>
    ///     全零
    /// </summary>
    Zeros,

    /// <summary>
    ///     全一
    /// </summary>
    Ones
}
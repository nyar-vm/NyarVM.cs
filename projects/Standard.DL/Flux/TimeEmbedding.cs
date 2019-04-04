namespace Std.DL.Flux;

/// <summary>
///     Sinusoidal 时间步嵌入 —— Stable Diffusion 时间条件注入
///     将离散时间步 t 映射为高维正弦嵌入向量
/// </summary>
public static class TimeEmbedding
{
    /// <summary>
    ///     生成 Sinusoidal 位置编码（与 Transformer 位置编码相同）
    /// </summary>
    /// <param name="timesteps">时间步数组 [batch, 1]，整数值</param>
    /// <param name="dim">嵌入维度</param>
    /// <param name="maxPeriod">最大周期（默认 10000）</param>
    /// <returns>正弦嵌入 [batch, dim]</returns>
    public static ArrayND Sinusoidal(ArrayND timesteps, int dim, float maxPeriod = 10000.0f)
    {
        var batch = timesteps.Shape[0];
        var halfDim = dim / 2;
        var result = ArrayND.Zeros(batch, dim);
        var spanT = timesteps.AsSpan();
        var spanR = result.AsWriteSpan();

        var logMax = MathF.Log(maxPeriod);
        for (var b = 0; b < batch; b++)
        {
            var t = spanT[b];
            for (var i = 0; i < halfDim; i++)
            {
                var freq = MathF.Exp(-logMax * (2.0f * i / dim));
                var arg = t * freq;
                spanR[b * dim + i] = MathF.Sin(arg);
                spanR[b * dim + i + halfDim] = MathF.Cos(arg);
            }

            if (dim % 2 != 0) spanR[b * dim + dim - 1] = 1.0f;
        }

        return result;
    }
}

/// <summary>
///     Time Embedding 层 —— 时间步 → MLP → 嵌入向量
///     Stable Diffusion 中注入到 ResBlock 的 scale/shift 参数
/// </summary>
public class TimeEmbeddingLayer : ILayer
{
    private readonly int _inputDim;
    private readonly int _outputDim;
    private readonly Dense _proj1;
    private readonly Dense _proj2;

    /// <summary>
    ///     创建时间嵌入层
    /// </summary>
    /// <param name="inputDim">Sin 嵌入输入维度</param>
    /// <param name="outputDim">输出维度（通常为通道数×2，对应 scale+shift）</param>
    public TimeEmbeddingLayer(int inputDim, int outputDim)
    {
        _inputDim = inputDim;
        _outputDim = outputDim;
        _proj1 = new Dense(inputDim, outputDim);
        _proj2 = new Dense(outputDim, outputDim);
    }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        return _proj1.Parameters().Concat(_proj2.Parameters());
    }

    /// <summary>
    ///     前向传播：timeEmb → Dense → SiLU → Dense
    /// </summary>
    /// <param name="timeEmb">正弦时间嵌入 [batch, inputDim]</param>
    /// <returns>时间条件向量 [batch, outputDim]</returns>
    public ArrayND Forward(ArrayND timeEmb)
    {
        var h = _proj1.forward(timeEmb);
        h = Activations.SiLUForward(h);
        return _proj2.forward(h);
    }
}
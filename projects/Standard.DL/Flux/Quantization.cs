namespace Std.DL.Flux;

/// <summary>
///     后训练量化 —— INT8/INT4 量化用于高效推理
///     将 FP32 权重量化为低精度整数，减少内存占用和加速推理
///     支持：对称量化、非对称量化、逐通道量化
/// </summary>
public static class Quantization
{
    /// <summary>
    ///     将 FP32 权重量化为 INT8（对称量化）
    ///     scale = max(|weight|) / 127
    ///     quantized = round(weight / scale)
    /// </summary>
    /// <param name="weight">FP32 权重</param>
    /// <returns>量化结果（量化值 + 缩放因子）</returns>
    public static QuantizedTensor QuantizeInt8(ArrayND weight)
    {
        var span = weight.AsSpan();
        var maxAbs = 0.0f;
        for (var i = 0; i < span.Length; i++)
        {
            var abs = MathF.Abs(span[i]);
            if (abs > maxAbs) maxAbs = abs;
        }

        var scale = maxAbs / 127.0f;
        if (scale < 1e-10f) scale = 1e-10f;

        var quantized = new sbyte[span.Length];
        for (var i = 0; i < span.Length; i++)
        {
            var q = MathF.Round(span[i] / scale);
            q = MathF.Max(-128, MathF.Min(127, q));
            quantized[i] = (sbyte)q;
        }

        return new QuantizedTensor(quantized, weight.Shape, scale, 0.0f, 8);
    }

    /// <summary>
    ///     将 FP32 权重量化为 INT8（非对称量化）
    ///     scale = (max - min) / 255
    ///     zero_point = round(-min / scale)
    ///     quantized = round(weight / scale) + zero_point
    /// </summary>
    /// <param name="weight">FP32 权重</param>
    /// <returns>量化结果</returns>
    public static QuantizedTensor QuantizeInt8Asymmetric(ArrayND weight)
    {
        var span = weight.AsSpan();
        var min = float.MaxValue;
        var max = float.NegativeInfinity;
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] < min) min = span[i];

            if (span[i] > max) max = span[i];
        }

        var scale = (max - min) / 255.0f;
        if (scale < 1e-10f) scale = 1e-10f;

        var zeroPoint = MathF.Round(-min / scale);
        zeroPoint = MathF.Max(0, MathF.Min(255, zeroPoint));

        var quantized = new byte[span.Length];
        for (var i = 0; i < span.Length; i++)
        {
            var q = MathF.Round(span[i] / scale) + zeroPoint;
            q = MathF.Max(0, MathF.Min(255, q));
            quantized[i] = (byte)q;
        }

        return new QuantizedTensor(quantized, weight.Shape, scale, zeroPoint, 8);
    }

    /// <summary>
    ///     将 FP32 权重量化为 INT4（对称量化）
    ///     scale = max(|weight|) / 7
    ///     quantized = round(weight / scale)，范围 [-8, 7]
    /// </summary>
    /// <param name="weight">FP32 权重</param>
    /// <returns>量化结果</returns>
    public static QuantizedTensor QuantizeInt4(ArrayND weight)
    {
        var span = weight.AsSpan();
        var maxAbs = 0.0f;
        for (var i = 0; i < span.Length; i++)
        {
            var abs = MathF.Abs(span[i]);
            if (abs > maxAbs) maxAbs = abs;
        }

        var scale = maxAbs / 7.0f;
        if (scale < 1e-10f) scale = 1e-10f;

        var quantized = new sbyte[span.Length];
        for (var i = 0; i < span.Length; i++)
        {
            var q = MathF.Round(span[i] / scale);
            q = MathF.Max(-8, MathF.Min(7, q));
            quantized[i] = (sbyte)q;
        }

        return new QuantizedTensor(quantized, weight.Shape, scale, 0.0f, 4);
    }

    /// <summary>
    ///     将量化权重反量化为 FP32
    ///     dequantized = (quantized - zero_point) × scale
    /// </summary>
    /// <param name="quantized">量化张量</param>
    /// <returns>FP32 权重</returns>
    public static ArrayND Dequantize(QuantizedTensor quantized)
    {
        var result = ArrayND.Zeros(quantized.Shape);
        var span = result.AsWriteSpan();

        if (quantized.IsSigned)
        {
            var data = (sbyte[])quantized.Data;
            for (var i = 0; i < data.Length && i < span.Length; i++)
                span[i] = (data[i] - quantized.ZeroPoint) * quantized.Scale;
        }
        else
        {
            var data = (byte[])quantized.Data;
            for (var i = 0; i < data.Length && i < span.Length; i++)
                span[i] = (data[i] - quantized.ZeroPoint) * quantized.Scale;
        }

        return result;
    }

    /// <summary>
    ///     计算量化误差（MSE）
    /// </summary>
    /// <param name="original">原始 FP32 权重</param>
    /// <param name="quantized">量化后反量化的权重</param>
    /// <returns>均方误差</returns>
    public static float QuantizationError(ArrayND original, ArrayND quantized)
    {
        var spanO = original.AsSpan();
        var spanQ = quantized.AsSpan();
        var mse = 0.0f;
        for (var i = 0; i < spanO.Length && i < spanQ.Length; i++)
        {
            var diff = spanO[i] - spanQ[i];
            mse += diff * diff;
        }

        return spanO.Length > 0 ? mse / spanO.Length : 0;
    }

    /// <summary>
    ///     逐通道 INT8 量化（每行独立量化）
    ///     适用于 Dense 层的权重矩阵 [fanIn, fanOut]
    /// </summary>
    /// <param name="weight">FP32 权重 [fanIn, fanOut]</param>
    /// <returns>逐通道量化结果列表</returns>
    public static ChannelQuantizedTensor QuantizeInt8PerChannel(ArrayND weight)
    {
        var rows = weight.Shape[0];
        var cols = weight.Shape[1];
        var scales = new float[rows];
        var zeroPoints = new float[rows];
        var quantizedData = new sbyte[rows * cols];

        var span = weight.AsSpan();

        for (var r = 0; r < rows; r++)
        {
            var maxAbs = 0.0f;
            for (var c = 0; c < cols; c++)
            {
                var abs = MathF.Abs(span[r * cols + c]);
                if (abs > maxAbs) maxAbs = abs;
            }

            var scale = maxAbs / 127.0f;
            if (scale < 1e-10f) scale = 1e-10f;

            scales[r] = scale;

            for (var c = 0; c < cols; c++)
            {
                var q = MathF.Round(span[r * cols + c] / scale);
                q = MathF.Max(-128, MathF.Min(127, q));
                quantizedData[r * cols + c] = (sbyte)q;
            }
        }

        return new ChannelQuantizedTensor(quantizedData, weight.Shape, scales, zeroPoints);
    }

    /// <summary>
    ///     逐通道反量化
    /// </summary>
    /// <param name="quantized">逐通道量化结果</param>
    /// <returns>FP32 权重</returns>
    public static ArrayND DequantizePerChannel(ChannelQuantizedTensor quantized)
    {
        var result = ArrayND.Zeros(quantized.Shape);
        var span = result.AsWriteSpan();
        var data = quantized.Data;
        var rows = quantized.Shape[0];
        var cols = quantized.Shape[1];

        for (var r = 0; r < rows; r++)
        for (var c = 0; c < cols; c++)
            span[r * cols + c] = data[r * cols + c] * quantized.Scales[r];

        return result;
    }
}

/// <summary>
///     量化张量 —— 存储量化后的权重和元数据
/// </summary>
public sealed class QuantizedTensor
{
    /// <summary>
    ///     创建量化张量
    /// </summary>
    /// <param name="data">量化数据</param>
    /// <param name="shape">原始形状</param>
    /// <param name="scale">缩放因子</param>
    /// <param name="zeroPoint">零点偏移</param>
    /// <param name="bits">量化位数</param>
    public QuantizedTensor(Array data, int[] shape, float scale, float zeroPoint, int bits)
    {
        Data = data;
        Shape = shape;
        Scale = scale;
        ZeroPoint = zeroPoint;
        Bits = bits;
    }

    /// <summary>
    ///     量化数据（sbyte[] 或 byte[]）
    /// </summary>
    public Array Data { get; }

    /// <summary>
    ///     原始形状
    /// </summary>
    public int[] Shape { get; }

    /// <summary>
    ///     缩放因子
    /// </summary>
    public float Scale { get; }

    /// <summary>
    ///     零点偏移
    /// </summary>
    public float ZeroPoint { get; }

    /// <summary>
    ///     量化位数（4 或 8）
    /// </summary>
    public int Bits { get; }

    /// <summary>
    ///     是否为有符号量化
    /// </summary>
    public bool IsSigned => Data is sbyte[];

    /// <summary>
    ///     压缩后字节数
    /// </summary>
    public int CompressedSize => Data.Length;

    /// <summary>
    ///     原始 FP32 字节数
    /// </summary>
    public int OriginalSize => Shape.Aggregate(1, (a, b) => a * b) * 4;

    /// <summary>
    ///     压缩比
    /// </summary>
    public float CompressionRatio => (float)OriginalSize / CompressedSize;
}

/// <summary>
///     逐通道量化张量
/// </summary>
public sealed class ChannelQuantizedTensor
{
    /// <summary>
    ///     创建逐通道量化张量
    /// </summary>
    /// <param name="data">量化数据</param>
    /// <param name="shape">原始形状</param>
    /// <param name="scales">缩放因子</param>
    /// <param name="zeroPoints">零点偏移</param>
    public ChannelQuantizedTensor(sbyte[] data, int[] shape, float[] scales, float[] zeroPoints)
    {
        Data = data;
        Shape = shape;
        Scales = scales;
        ZeroPoints = zeroPoints;
    }

    /// <summary>
    ///     量化数据
    /// </summary>
    public sbyte[] Data { get; }

    /// <summary>
    ///     原始形状
    /// </summary>
    public int[] Shape { get; }

    /// <summary>
    ///     逐通道缩放因子
    /// </summary>
    public float[] Scales { get; }

    /// <summary>
    ///     逐通道零点偏移
    /// </summary>
    public float[] ZeroPoints { get; }
}
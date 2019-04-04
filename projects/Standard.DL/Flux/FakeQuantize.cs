namespace Std.DL.Flux;

/// <summary>
///     伪量化节点 —— 在训练期间模拟量化误差
///     前向传播执行量化-反量化操作，引入量化噪声
///     反向传播使用直通估计器（STE），梯度直接传递
/// </summary>
public sealed class FakeQuantize
{
    /// <summary>
    ///     创建伪量化节点
    /// </summary>
    /// <param name="numBits">量化位数，默认 8</param>
    /// <param name="minVal">量化范围下界，默认 -1.0</param>
    /// <param name="maxVal">量化范围上界，默认 1.0</param>
    public FakeQuantize(int numBits = 8, float minVal = -1.0f, float maxVal = 1.0f)
    {
        if (numBits is < 1 or > 32) throw new ArgumentOutOfRangeException(nameof(numBits), "量化位数必须在 1 到 32 之间");

        if (minVal >= maxVal) throw new ArgumentException("量化范围下界必须小于上界");

        NumBits = numBits;
        MinVal = minVal;
        MaxVal = maxVal;
    }

    /// <summary>
    ///     量化位数
    /// </summary>
    public int NumBits { get; }

    /// <summary>
    ///     量化范围下界
    /// </summary>
    public float MinVal { get; private set; }

    /// <summary>
    ///     量化范围上界
    /// </summary>
    public float MaxVal { get; private set; }

    /// <summary>
    ///     前向传播：执行伪量化（量化后反量化）
    ///     Scale = (maxVal - minVal) / (2^numBits - 1)
    ///     Quantized = round((input - minVal) / scale)
    ///     Clamped = clamp(quantized, 0, 2^numBits - 1)
    ///     Output = clamped * scale + minVal
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <returns>伪量化后的张量</returns>
    public ArrayND Forward(ArrayND input)
    {
        var maxLevel = (1 << NumBits) - 1;
        var scale = (MaxVal - MinVal) / maxLevel;

        if (scale < 1e-10f) scale = 1e-10f;

        var result = ArrayND.Zeros(input.Shape);
        var spanInput = input.AsSpan();
        var spanResult = result.AsWriteSpan();

        for (var i = 0; i < spanInput.Length; i++)
        {
            var quantized = MathF.Round((spanInput[i] - MinVal) / scale);
            var clamped = MathF.Max(0, MathF.Min(maxLevel, quantized));
            spanResult[i] = clamped * scale + MinVal;
        }

        return result;
    }

    /// <summary>
    ///     前向传播（带自动微分记录）：使用直通估计器（STE）
    ///     前向：与无微分版本相同，执行伪量化
    ///     反向：梯度直接传递（STE 近似），忽略量化操作的不可导性
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>伪量化后的张量</returns>
    public ArrayND Forward(ArrayND input, AutogradContext ctx)
    {
        var output = Forward(input);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];

            if (dOutput is null) return [null];

            var dInput = ArrayND.Zeros(input.Shape);
            var spanDOutput = dOutput.AsSpan();
            var spanDInput = dInput.AsWriteSpan();
            var spanInput = input.AsSpan();

            var maxLevel = (1 << NumBits) - 1;
            var scale = (MaxVal - MinVal) / maxLevel;

            if (scale < 1e-10f) scale = 1e-10f;

            for (var i = 0; i < spanInput.Length; i++)
            {
                var quantized = (spanInput[i] - MinVal) / scale;
                if (quantized < 0 || quantized > maxLevel)
                    spanDInput[i] = 0.0f;
                else
                    spanDInput[i] = spanDOutput[i];
            }

            return [dInput];
        });

        return output;
    }

    /// <summary>
    ///     根据观测数据更新量化范围
    ///     取输入张量的最小值和最大值作为新的量化范围
    /// </summary>
    /// <param name="input">观测数据张量</param>
    public void UpdateRange(ArrayND input)
    {
        var span = input.AsSpan();
        var observedMin = float.MaxValue;
        var observedMax = float.NegativeInfinity;

        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] < observedMin) observedMin = span[i];

            if (span[i] > observedMax) observedMax = span[i];
        }

        MinVal = observedMin;
        MaxVal = observedMax;
    }
}
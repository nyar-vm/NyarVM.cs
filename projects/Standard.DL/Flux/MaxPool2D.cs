namespace Std.DL.Flux;

/// <summary>
///     二维最大池化
/// </summary>
public class MaxPool2D
{
    private readonly int _poolSize;
    private readonly int _stride;

    /// <summary>
    ///     创建最大池化层
    /// </summary>
    /// <param name="poolSize">池化窗口大小</param>
    /// <param name="stride">步幅</param>
    public MaxPool2D(int poolSize = 2, int stride = 2)
    {
        _poolSize = poolSize;
        _stride = stride;
    }

    /// <summary>
    ///     前向传播
    /// </summary>
    /// <param name="input">输入张量 [batch, channels * h * w]</param>
    /// <param name="channels">通道数</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <returns>输出张量 [batch, channels * outH * outW] 和输出尺寸</returns>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int channels, int inH, int inW)
    {
        var batch = input.Shape[0];
        var outH = inH / _stride;
        var outW = inW / _stride;

        var output = ArrayND.Zeros(batch, channels * outH * outW);
        var spanIn = input.AsSpan();
        var spanOut = output.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var c = 0; c < channels; c++)
        for (var oh = 0; oh < outH; oh++)
        for (var ow = 0; ow < outW; ow++)
        {
            var ih = oh * _stride;
            var iw = ow * _stride;

            var baseIdx = n * channels * inH * inW + c * inH * inW;
            var max = float.MinValue;
            for (var dh = 0; dh < _poolSize; dh++)
            for (var dw = 0; dw < _poolSize; dw++)
            {
                var val = spanIn[baseIdx + (ih + dh) * inW + iw + dw];
                if (val > max) max = val;
            }

            spanOut[n * channels * outH * outW + c * outH * outW + oh * outW + ow] = max;
        }

        return (output, outH, outW);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int channels, int inH, int inW,
        AutogradContext ctx)
    {
        var (output, outH, outW) = Forward(input, channels, inH, inW);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var batch = input.Shape[0];
            var dInput = ArrayND.Zeros(batch, channels * inH * inW);

            var spanIn = input.AsSpan();
            var spanDOut = dOutput.AsSpan();
            var spanDIn = dInput.AsWriteSpan();

            for (var n = 0; n < batch; n++)
            for (var c = 0; c < channels; c++)
            for (var oh = 0; oh < outH; oh++)
            for (var ow = 0; ow < outW; ow++)
            {
                var ih = oh * _stride;
                var iw = ow * _stride;

                var baseIdx = n * channels * inH * inW + c * inH * inW;
                var maxVal = float.MinValue;
                var maxDh = 0;
                var maxDw = 0;

                for (var dh = 0; dh < _poolSize; dh++)
                for (var dw = 0; dw < _poolSize; dw++)
                {
                    var val = spanIn[baseIdx + (ih + dh) * inW + iw + dw];
                    if (val > maxVal)
                    {
                        maxVal = val;
                        maxDh = dh;
                        maxDw = dw;
                    }
                }

                var grad = spanDOut[n * channels * outH * outW + c * outH * outW + oh * outW + ow];
                spanDIn[baseIdx + (ih + maxDh) * inW + iw + maxDw] += grad;
            }

            return [dInput];
        });

        return (output, outH, outW);
    }
}
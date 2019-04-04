namespace Std.DL.Flux;

/// <summary>
///     二维最近邻上采样 —— 将空间维度放大 scale 倍
///     用于扩散模型解码器、U-Net 上采样路径
/// </summary>
public sealed class Upsample2D
{
    private readonly int _scaleFactor;

    /// <summary>
    ///     创建二维上采样层
    /// </summary>
    /// <param name="scaleFactor">上采样倍率</param>
    public Upsample2D(int scaleFactor = 2)
    {
        _scaleFactor = scaleFactor;
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
        var outH = inH * _scaleFactor;
        var outW = inW * _scaleFactor;

        var output = ArrayND.Zeros(batch, channels * outH * outW);
        var spanIn = input.AsSpan();
        var spanOut = output.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var c = 0; c < channels; c++)
        for (var oh = 0; oh < outH; oh++)
        for (var ow = 0; ow < outW; ow++)
        {
            var ih = oh / _scaleFactor;
            var iw = ow / _scaleFactor;
            var inIdx = n * channels * inH * inW + c * inH * inW + ih * inW + iw;
            var outIdx = n * channels * outH * outW + c * outH * outW + oh * outW + ow;
            spanOut[outIdx] = spanIn[inIdx];
        }

        return (output, outH, outW);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int channels, int inH, int inW,
        AutogradContext ctx)
    {
        var batch = input.Shape[0];
        var outH = inH * _scaleFactor;
        var outW = inW * _scaleFactor;

        var output = ArrayND.Zeros(batch, channels * outH * outW);
        var spanIn = input.AsSpan();
        var spanOut = output.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var c = 0; c < channels; c++)
        for (var oh = 0; oh < outH; oh++)
        for (var ow = 0; ow < outW; ow++)
        {
            var ih = oh / _scaleFactor;
            var iw = ow / _scaleFactor;
            var inIdx = n * channels * inH * inW + c * inH * inW + ih * inW + iw;
            var outIdx = n * channels * outH * outW + c * outH * outW + oh * outW + ow;
            spanOut[outIdx] = spanIn[inIdx];
        }

        var savedInH = inH;
        var savedInW = inW;
        var savedChannels = channels;

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var spanDOut = dOutput.AsSpan();

            var dInput = ArrayND.Zeros(batch, savedChannels * savedInH * savedInW);
            var spanDIn = dInput.AsWriteSpan();

            for (var n = 0; n < batch; n++)
            for (var c = 0; c < savedChannels; c++)
            for (var oh = 0; oh < outH; oh++)
            for (var ow = 0; ow < outW; ow++)
            {
                var ih = oh / _scaleFactor;
                var iw = ow / _scaleFactor;
                var outIdx = n * savedChannels * outH * outW + c * outH * outW + oh * outW + ow;
                var inIdx = n * savedChannels * savedInH * savedInW + c * savedInH * savedInW + ih * savedInW + iw;
                spanDIn[inIdx] += spanDOut[outIdx];
            }

            return [dInput];
        });

        return (output, outH, outW);
    }
}
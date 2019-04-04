using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     逐通道二维卷积层 —— 每个输入通道使用独立卷积核，不跨通道混合
///     参数和计算量远小于标准卷积，是 MobileNet 等轻量网络的核心组件
/// </summary>
public sealed class DepthwiseConv2D : ILayer, ITrainableModel
{
    private readonly int _inH;
    private readonly int _inW;

    /// <summary>
    ///     创建逐通道二维卷积层
    /// </summary>
    /// <param name="channels">通道数</param>
    /// <param name="kernelSize">卷积核大小</param>
    /// <param name="stride">步幅</param>
    /// <param name="padding">填充</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    public DepthwiseConv2D(int channels, int kernelSize, int stride = 1, int padding = 0, int inH = 28, int inW = 28)
    {
        Channels = channels;
        KernelSize = kernelSize;
        Stride = stride;
        Padding = padding;
        _inH = inH;
        _inW = inW;

        var fanIn = kernelSize * kernelSize;
        Weight = ArrayND.HeNormal(fanIn, channels * kernelSize * kernelSize);
        Weight = Weight.Reshape(channels, kernelSize * kernelSize);
        Bias = ArrayND.Zeros(channels);
    }

    /// <summary>逐通道卷积核权重 [channels, kernelSize * kernelSize]</summary>
    public ArrayND Weight { get; }

    /// <summary>偏置 [channels]</summary>
    public ArrayND Bias { get; }

    /// <summary>通道数（输入与输出相同）</summary>
    public int Channels { get; }

    /// <summary>卷积核大小</summary>
    public int KernelSize { get; }

    /// <summary>步幅</summary>
    public int Stride { get; }

    /// <summary>填充</summary>
    public int Padding { get; }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        yield return new Parameter(Weight);
        yield return new Parameter(Bias);
    }

    /// <summary>
    ///     ITrainableModel 接口：无上下文前向
    /// </summary>
    ArrayND ITrainableModel.forward(ArrayND input)
    {
        var (output, _, _) = Forward(input, _inH, _inW);
        return output;
    }

    /// <summary>
    ///     ITrainableModel 接口：带自动微分上下文的前向
    /// </summary>
    ArrayND ITrainableModel.forward(ArrayND input, AutogradContext ctx)
    {
        var (output, _, _) = Forward(input, _inH, _inW, ctx);
        return output;
    }

    /// <summary>
    ///     前向传播
    /// </summary>
    /// <param name="input">输入张量 [batch, channels * h * w]</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <returns>输出张量 [batch, channels * outH * outW] 和输出尺寸</returns>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int inH, int inW)
    {
        var batch = input.Shape[0];
        var outH = (inH + 2 * Padding - KernelSize) / Stride + 1;
        var outW = (inW + 2 * Padding - KernelSize) / Stride + 1;

        var output = ArrayND.Zeros(batch, Channels * outH * outW);
        var spanInput = input.AsSpan();
        var spanWeight = Weight.AsSpan();
        var spanBias = Bias.AsSpan();
        var spanOutput = output.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var c = 0; c < Channels; c++)
        for (var oh = 0; oh < outH; oh++)
        for (var ow = 0; ow < outW; ow++)
        {
            var sum = spanBias[c];
            for (var kh = 0; kh < KernelSize; kh++)
            for (var kw = 0; kw < KernelSize; kw++)
            {
                var ih = oh * Stride - Padding + kh;
                var iw = ow * Stride - Padding + kw;
                if (ih >= 0 && ih < inH && iw >= 0 && iw < inW)
                {
                    var inputIdx = n * Channels * inH * inW + c * inH * inW + ih * inW + iw;
                    var weightIdx = c * KernelSize * KernelSize + kh * KernelSize + kw;
                    sum += spanInput[inputIdx] * spanWeight[weightIdx];
                }
            }

            var outputIdx = n * Channels * outH * outW + c * outH * outW + oh * outW + ow;
            spanOutput[outputIdx] = sum;
        }

        return (output, outH, outW);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="input">输入张量 [batch, channels * h * w]</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>输出张量和输出尺寸</returns>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int inH, int inW, AutogradContext ctx)
    {
        var batch = input.Shape[0];
        var outH = (inH + 2 * Padding - KernelSize) / Stride + 1;
        var outW = (inW + 2 * Padding - KernelSize) / Stride + 1;

        var output = ArrayND.Zeros(batch, Channels * outH * outW);
        var spanInput = input.AsSpan();
        var spanWeight = Weight.AsSpan();
        var spanBias = Bias.AsSpan();
        var spanOutput = output.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var c = 0; c < Channels; c++)
        for (var oh = 0; oh < outH; oh++)
        for (var ow = 0; ow < outW; ow++)
        {
            var sum = spanBias[c];
            for (var kh = 0; kh < KernelSize; kh++)
            for (var kw = 0; kw < KernelSize; kw++)
            {
                var ih = oh * Stride - Padding + kh;
                var iw = ow * Stride - Padding + kw;
                if (ih >= 0 && ih < inH && iw >= 0 && iw < inW)
                {
                    var inputIdx = n * Channels * inH * inW + c * inH * inW + ih * inW + iw;
                    var weightIdx = c * KernelSize * KernelSize + kh * KernelSize + kw;
                    sum += spanInput[inputIdx] * spanWeight[weightIdx];
                }
            }

            var outputIdx = n * Channels * outH * outW + c * outH * outW + oh * outW + ow;
            spanOutput[outputIdx] = sum;
        }

        var savedInH = inH;
        var savedInW = inW;
        var savedOutH = outH;
        var savedOutW = outW;
        var savedInput = input;
        var savedWeight = Weight;

        ctx.Record(output, [input, Weight, Bias], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var spanDOut = dOutput.AsSpan();

            var dInput = ArrayND.Zeros(batch, Channels * savedInH * savedInW);
            var spanDIn = dInput.AsWriteSpan();

            var dWeight = ArrayND.Zeros(Channels * KernelSize * KernelSize);
            var spanDW = dWeight.AsWriteSpan();

            var dBias = ArrayND.Zeros(Channels);
            var spanDB = dBias.AsWriteSpan();

            var si = savedInput.AsSpan();
            var sw = savedWeight.AsSpan();

            for (var n = 0; n < batch; n++)
            for (var c = 0; c < Channels; c++)
            for (var oh = 0; oh < savedOutH; oh++)
            for (var ow = 0; ow < savedOutW; ow++)
            {
                var dOutIdx = n * Channels * savedOutH * savedOutW + c * savedOutH * savedOutW + oh * savedOutW + ow;
                var dVal = spanDOut[dOutIdx];

                spanDB[c] += dVal;

                for (var kh = 0; kh < KernelSize; kh++)
                for (var kw = 0; kw < KernelSize; kw++)
                {
                    var ih = oh * Stride - Padding + kh;
                    var iw = ow * Stride - Padding + kw;

                    if (ih >= 0 && ih < savedInH && iw >= 0 && iw < savedInW)
                    {
                        var inIdx = n * Channels * savedInH * savedInW + c * savedInH * savedInW + ih * savedInW + iw;
                        var wIdx = c * KernelSize * KernelSize + kh * KernelSize + kw;

                        spanDW[wIdx] += si[inIdx] * dVal;
                        spanDIn[inIdx] += sw[wIdx] * dVal;
                    }
                }
            }

            return [dInput, dWeight.Reshape(Channels, KernelSize * KernelSize), dBias];
        });

        return (output, outH, outW);
    }

    /// <summary>
    ///     获取输出展平尺寸
    /// </summary>
    public int GetFlatSize(int inH, int inW)
    {
        var outH = (inH + 2 * Padding - KernelSize) / Stride + 1;
        var outW = (inW + 2 * Padding - KernelSize) / Stride + 1;
        return Channels * outH * outW;
    }
}
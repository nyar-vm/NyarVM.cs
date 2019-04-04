using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     二维转置卷积层（反卷积 / 分数步长卷积）
///     用于上采样特征图，是 Stable Diffusion 等 U-Net 解码器的核心组件
///     数学上等价于：对输入做零填充插入后做标准卷积
/// </summary>
public sealed class ConvTranspose2D : ILayer, ITrainableModel
{
    private readonly int _inH;
    private readonly int _inW;

    /// <summary>
    ///     创建二维转置卷积层
    /// </summary>
    /// <param name="inChannels">输入通道数</param>
    /// <param name="outChannels">输出通道数</param>
    /// <param name="kernelSize">卷积核大小</param>
    /// <param name="stride">步幅</param>
    /// <param name="padding">填充</param>
    /// <param name="outputPadding">输出填充</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    public ConvTranspose2D(int inChannels, int outChannels, int kernelSize, int stride = 1, int padding = 0,
        int outputPadding = 0, int inH = 28, int inW = 28)
    {
        InChannels = inChannels;
        OutChannels = outChannels;
        KernelSize = kernelSize;
        Stride = stride;
        Padding = padding;
        OutputPadding = outputPadding;
        _inH = inH;
        _inW = inW;

        var fanIn = inChannels * kernelSize * kernelSize;
        Weight = ArrayND.HeNormal(fanIn, inChannels * outChannels * kernelSize * kernelSize);
        Weight = Weight.Reshape(inChannels, outChannels * kernelSize * kernelSize);
        Bias = ArrayND.Zeros(outChannels);
    }

    /// <summary>转置卷积核权重 [inChannels, outChannels * kH * kW]</summary>
    public ArrayND Weight { get; }

    /// <summary>偏置 [outChannels]</summary>
    public ArrayND Bias { get; }

    /// <summary>输入通道数</summary>
    public int InChannels { get; }

    /// <summary>输出通道数</summary>
    public int OutChannels { get; }

    /// <summary>卷积核大小</summary>
    public int KernelSize { get; }

    /// <summary>步幅</summary>
    public int Stride { get; }

    /// <summary>填充</summary>
    public int Padding { get; }

    /// <summary>输出填充</summary>
    public int OutputPadding { get; }

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
    /// <param name="input">输入张量 [batch, inChannels * h * w]</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <returns>输出张量 [batch, outChannels * outH * outW] 和输出尺寸</returns>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int inH, int inW)
    {
        var batch = input.Shape[0];
        var outH = (inH - 1) * Stride - 2 * Padding + KernelSize + OutputPadding;
        var outW = (inW - 1) * Stride - 2 * Padding + KernelSize + OutputPadding;

        var output = ArrayND.Zeros(batch, OutChannels * outH * outW);
        var spanInput = input.AsSpan();
        var spanWeight = Weight.AsSpan();
        var spanBias = Bias.AsSpan();
        var spanOutput = output.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            for (var ic = 0; ic < InChannels; ic++)
            for (var ih = 0; ih < inH; ih++)
            for (var iw = 0; iw < inW; iw++)
            {
                var inputIdx = n * InChannels * inH * inW + ic * inH * inW + ih * inW + iw;
                var xVal = spanInput[inputIdx];

                for (var oc = 0; oc < OutChannels; oc++)
                for (var kh = 0; kh < KernelSize; kh++)
                for (var kw = 0; kw < KernelSize; kw++)
                {
                    var oh = ih * Stride - Padding + kh;
                    var ow = iw * Stride - Padding + kw;

                    if (oh >= 0 && oh < outH && ow >= 0 && ow < outW)
                    {
                        var weightIdx = ic * OutChannels * KernelSize * KernelSize + oc * KernelSize * KernelSize +
                                        kh * KernelSize + kw;
                        var outputIdx = n * OutChannels * outH * outW + oc * outH * outW + oh * outW + ow;
                        spanOutput[outputIdx] += xVal * spanWeight[weightIdx];
                    }
                }
            }

            for (var oc = 0; oc < OutChannels; oc++)
            for (var oh = 0; oh < outH; oh++)
            for (var ow = 0; ow < outW; ow++)
            {
                var outputIdx = n * OutChannels * outH * outW + oc * outH * outW + oh * outW + ow;
                spanOutput[outputIdx] += spanBias[oc];
            }
        }

        return (output, outH, outW);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int inH, int inW, AutogradContext ctx)
    {
        var batch = input.Shape[0];
        var outH = (inH - 1) * Stride - 2 * Padding + KernelSize + OutputPadding;
        var outW = (inW - 1) * Stride - 2 * Padding + KernelSize + OutputPadding;

        var output = ArrayND.Zeros(batch, OutChannels * outH * outW);
        var spanInput = input.AsSpan();
        var spanWeight = Weight.AsSpan();
        var spanBias = Bias.AsSpan();
        var spanOutput = output.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            for (var ic = 0; ic < InChannels; ic++)
            for (var ih = 0; ih < inH; ih++)
            for (var iw = 0; iw < inW; iw++)
            {
                var inputIdx = n * InChannels * inH * inW + ic * inH * inW + ih * inW + iw;
                var xVal = spanInput[inputIdx];

                for (var oc = 0; oc < OutChannels; oc++)
                for (var kh = 0; kh < KernelSize; kh++)
                for (var kw = 0; kw < KernelSize; kw++)
                {
                    var oh = ih * Stride - Padding + kh;
                    var ow = iw * Stride - Padding + kw;

                    if (oh >= 0 && oh < outH && ow >= 0 && ow < outW)
                    {
                        var weightIdx = ic * OutChannels * KernelSize * KernelSize + oc * KernelSize * KernelSize +
                                        kh * KernelSize + kw;
                        var outputIdx = n * OutChannels * outH * outW + oc * outH * outW + oh * outW + ow;
                        spanOutput[outputIdx] += xVal * spanWeight[weightIdx];
                    }
                }
            }

            for (var oc = 0; oc < OutChannels; oc++)
            for (var oh = 0; oh < outH; oh++)
            for (var ow = 0; ow < outW; ow++)
            {
                var outputIdx = n * OutChannels * outH * outW + oc * outH * outW + oh * outW + ow;
                spanOutput[outputIdx] += spanBias[oc];
            }
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

            var dInput = ArrayND.Zeros(batch, InChannels * savedInH * savedInW);
            var spanDIn = dInput.AsWriteSpan();

            var dWeight = ArrayND.Zeros(InChannels * OutChannels * KernelSize * KernelSize);
            var spanDW = dWeight.AsWriteSpan();

            var dBias = ArrayND.Zeros(OutChannels);
            var spanDB = dBias.AsWriteSpan();

            var si = savedInput.AsSpan();
            var sw = savedWeight.AsSpan();

            for (var n = 0; n < batch; n++)
            {
                for (var ic = 0; ic < InChannels; ic++)
                for (var ih = 0; ih < savedInH; ih++)
                for (var iw = 0; iw < savedInW; iw++)
                for (var oc = 0; oc < OutChannels; oc++)
                for (var kh = 0; kh < KernelSize; kh++)
                for (var kw = 0; kw < KernelSize; kw++)
                {
                    var oh = ih * Stride - Padding + kh;
                    var ow = iw * Stride - Padding + kw;

                    if (oh >= 0 && oh < savedOutH && ow >= 0 && ow < savedOutW)
                    {
                        var outputIdx = n * OutChannels * savedOutH * savedOutW + oc * savedOutH * savedOutW +
                                        oh * savedOutW + ow;
                        var dVal = spanDOut[outputIdx];
                        var weightIdx = ic * OutChannels * KernelSize * KernelSize + oc * KernelSize * KernelSize +
                                        kh * KernelSize + kw;
                        var inputIdx = n * InChannels * savedInH * savedInW + ic * savedInH * savedInW + ih * savedInW +
                                       iw;

                        spanDIn[inputIdx] += sw[weightIdx] * dVal;
                        spanDW[weightIdx] += si[inputIdx] * dVal;
                    }
                }

                for (var oc = 0; oc < OutChannels; oc++)
                for (var oh = 0; oh < savedOutH; oh++)
                for (var ow = 0; ow < savedOutW; ow++)
                {
                    var outputIdx = n * OutChannels * savedOutH * savedOutW + oc * savedOutH * savedOutW +
                                    oh * savedOutW + ow;
                    spanDB[oc] += spanDOut[outputIdx];
                }
            }

            return [dInput, dWeight.Reshape(InChannels, OutChannels * KernelSize * KernelSize), dBias];
        });

        return (output, outH, outW);
    }

    /// <summary>
    ///     获取输出展平尺寸
    /// </summary>
    public int GetFlatSize(int inH, int inW)
    {
        var outH = (inH - 1) * Stride - 2 * Padding + KernelSize + OutputPadding;
        var outW = (inW - 1) * Stride - 2 * Padding + KernelSize + OutputPadding;
        return OutChannels * outH * outW;
    }
}
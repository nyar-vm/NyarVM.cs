using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     二维卷积层
/// </summary>
public sealed class Conv2D : ILayer, ITrainableModel
{
    private readonly int _inH;
    private readonly int _inW;

    /// <summary>
    ///     创建二维卷积层
    /// </summary>
    /// <param name="inChannels">输入通道数</param>
    /// <param name="outChannels">输出通道数</param>
    /// <param name="kernelSize">卷积核大小</param>
    /// <param name="stride">步幅</param>
    /// <param name="padding">填充</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    public Conv2D(int inChannels, int outChannels, int kernelSize, int stride = 1, int padding = 0, int inH = 28,
        int inW = 28)
    {
        InChannels = inChannels;
        OutChannels = outChannels;
        KernelSize = kernelSize;
        Stride = stride;
        Padding = padding;
        _inH = inH;
        _inW = inW;

        var fanIn = inChannels * kernelSize * kernelSize;
        Weight = ArrayND.HeNormal(fanIn, outChannels * inChannels * kernelSize * kernelSize);
        Weight = Weight.Reshape(outChannels, inChannels * kernelSize * kernelSize);
        Bias = ArrayND.Zeros(outChannels);
    }

    /// <summary>卷积核权重 [outChannels, inChannels * kH * kW]</summary>
    public ArrayND Weight { get; }

    /// <summary>偏置 [outChannels]</summary>
    public ArrayND Bias { get; }

    /// <summary>输入通道数</summary>
    public int InChannels { get; }

    /// <summary>输出通道数</summary>
    public int OutChannels { get; }

    /// <summary>卷积核高度</summary>
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
    ///     ITrainableModel 接口：无上下文前向（用于 Trainer）
    /// </summary>
    /// <param name="input">输入张量 [batch, inChannels * h * w]</param>
    /// <returns>输出张量 [batch, outChannels * outH * outW]</returns>
    ArrayND ITrainableModel.forward(ArrayND input)
    {
        var (output, _, _) = Forward(input, _inH, _inW);
        return output;
    }

    /// <summary>
    ///     ITrainableModel 接口：带自动微分上下文的前向
    /// </summary>
    /// <param name="input">输入张量 [batch, inChannels * h * w]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>输出张量 [batch, outChannels * outH * outW]</returns>
    ArrayND ITrainableModel.forward(ArrayND input, AutogradContext ctx)
    {
        var (output, _, _) = Forward(input, _inH, _inW, ctx);
        return output;
    }

    /// <summary>
    ///     前向传播：使用 im2col 方法实现卷积
    /// </summary>
    /// <param name="input">输入张量 [batch, inChannels * h * w]</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <returns>输出张量 [batch, outChannels * outH * outW] 和输出尺寸</returns>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int inH, int inW)
    {
        var batch = input.Shape[0];
        var outH = (inH + 2 * Padding - KernelSize) / Stride + 1;
        var outW = (inW + 2 * Padding - KernelSize) / Stride + 1;

        var col = Im2Col(input, inH, inW, outH, outW, batch);
        var weightT = Weight.Transpose();
        var gemmResult = ArrayND.MatMul(col, weightT);

        var output = ArrayND.Zeros(batch, OutChannels * outH * outW);
        var spanGemm = gemmResult.AsSpan();
        var spanBias = Bias.AsSpan();
        var spanOutput = output.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var oc = 0; oc < OutChannels; oc++)
        for (var oh = 0; oh < outH * outW; oh++)
            spanOutput[n * OutChannels * outH * outW + oc * outH * outW + oh] =
                spanGemm[n * outH * outW * OutChannels + oh * OutChannels + oc] + spanBias[oc];

        return (output, outH, outW);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="input">输入张量 [batch, inChannels * h * w]</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>输出张量和输出尺寸</returns>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int inH, int inW, AutogradContext ctx)
    {
        var batch = input.Shape[0];
        var outH = (inH + 2 * Padding - KernelSize) / Stride + 1;
        var outW = (inW + 2 * Padding - KernelSize) / Stride + 1;

        var col = Im2Col(input, inH, inW, outH, outW, batch);
        var weightT = Weight.Transpose();
        var gemmResult = ArrayND.MatMul(col, weightT);

        var output = ArrayND.Zeros(batch, OutChannels * outH * outW);
        var spanGemm = gemmResult.AsSpan();
        var spanBias = Bias.AsSpan();
        var spanOutput = output.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var oc = 0; oc < OutChannels; oc++)
        for (var oh = 0; oh < outH * outW; oh++)
            spanOutput[n * OutChannels * outH * outW + oc * outH * outW + oh] =
                spanGemm[n * outH * outW * OutChannels + oh * OutChannels + oc] + spanBias[oc];

        var savedInH = inH;
        var savedInW = inW;
        var savedOutH = outH;
        var savedOutW = outW;

        ctx.Record(output, [input, Weight, Bias], outputGrads =>
        {
            var dOutput = outputGrads[0];

            var dGemm = Col2ImOutput(dOutput, savedOutH, savedOutW, batch);
            var dCol = ArrayND.MatMul(dGemm, Weight);
            var dWeight = ArrayND.MatMul(dGemm.Transpose(), col);

            var dInput = Col2Im(dCol, savedInH, savedInW, savedOutH, savedOutW, batch);

            var dBias = ArrayND.Zeros(OutChannels);
            var spanDOutput = dOutput.AsSpan();
            var spanDBias = dBias.AsWriteSpan();
            for (var n = 0; n < batch; n++)
            for (var oc = 0; oc < OutChannels; oc++)
            for (var s = 0; s < savedOutH * savedOutW; s++)
                spanDBias[oc] += spanDOutput[n * OutChannels * savedOutH * savedOutW + oc * savedOutH * savedOutW + s];

            return [dInput, dWeight, dBias];
        });

        return (output, outH, outW);
    }

    /// <summary>
    ///     获取卷积输出的展平尺寸
    /// </summary>
    public int GetFlatSize(int inH, int inW)
    {
        var outH = (inH + 2 * Padding - KernelSize) / Stride + 1;
        var outW = (inW + 2 * Padding - KernelSize) / Stride + 1;
        return OutChannels * outH * outW;
    }

    private ArrayND Im2Col(ArrayND input, int inH, int inW, int outH, int outW, int batch)
    {
        var colRows = batch * outH * outW;
        var colCols = InChannels * KernelSize * KernelSize;
        var col = ArrayND.Zeros(colRows, colCols);

        var spanInput = input.AsSpan();
        var spanCol = col.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var oh = 0; oh < outH; oh++)
        for (var ow = 0; ow < outW; ow++)
        {
            var rowIdx = n * outH * outW + oh * outW + ow;
            for (var ic = 0; ic < InChannels; ic++)
            for (var kh = 0; kh < KernelSize; kh++)
            for (var kw = 0; kw < KernelSize; kw++)
            {
                var ih = oh * Stride - Padding + kh;
                var iw = ow * Stride - Padding + kw;

                var colIdx = ic * KernelSize * KernelSize + kh * KernelSize + kw;

                if (ih >= 0 && ih < inH && iw >= 0 && iw < inW)
                {
                    var inputIdx = n * InChannels * inH * inW + ic * inH * inW + ih * inW + iw;
                    spanCol[rowIdx * colCols + colIdx] = spanInput[inputIdx];
                }
            }
        }

        return col;
    }

    /// <summary>
    ///     将输出梯度从 NCHW 格式转为 im2col 矩阵格式
    /// </summary>
    private ArrayND Col2ImOutput(ArrayND dOutput, int outH, int outW, int batch)
    {
        var rows = batch * outH * outW;
        var cols = OutChannels;
        var result = ArrayND.Zeros(rows, cols);

        var spanDOutput = dOutput.AsSpan();
        var spanResult = result.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var oh = 0; oh < outH; oh++)
        for (var ow = 0; ow < outW; ow++)
        {
            var rowIdx = n * outH * outW + oh * outW + ow;
            for (var oc = 0; oc < OutChannels; oc++)
                spanResult[rowIdx * cols + oc] =
                    spanDOutput[n * OutChannels * outH * outW + oc * outH * outW + oh * outW + ow];
        }

        return result;
    }

    /// <summary>
    ///     col2im：将 im2col 矩阵转回图像格式（用于反向传播）
    /// </summary>
    private ArrayND Col2Im(ArrayND col, int inH, int inW, int outH, int outW, int batch)
    {
        var result = ArrayND.Zeros(batch, InChannels * inH * inW);
        var spanCol = col.AsSpan();
        var spanResult = result.AsWriteSpan();
        var colCols = InChannels * KernelSize * KernelSize;

        for (var n = 0; n < batch; n++)
        for (var oh = 0; oh < outH; oh++)
        for (var ow = 0; ow < outW; ow++)
        {
            var rowIdx = n * outH * outW + oh * outW + ow;
            for (var ic = 0; ic < InChannels; ic++)
            for (var kh = 0; kh < KernelSize; kh++)
            for (var kw = 0; kw < KernelSize; kw++)
            {
                var ih = oh * Stride - Padding + kh;
                var iw = ow * Stride - Padding + kw;

                var colIdx = ic * KernelSize * KernelSize + kh * KernelSize + kw;

                if (ih >= 0 && ih < inH && iw >= 0 && iw < inW)
                {
                    var inputIdx = n * InChannels * inH * inW + ic * inH * inW + ih * inW + iw;
                    spanResult[inputIdx] += spanCol[rowIdx * colCols + colIdx];
                }
            }
        }

        return result;
    }
}
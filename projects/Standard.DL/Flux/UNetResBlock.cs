using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     U-Net 残差块 —— Stable Diffusion 的核心构建块
///     结构：GroupNorm → SiLU → Conv2D → 时间条件注入 → GroupNorm → SiLU → Conv2D + 残差
///     时间条件通过 scale + shift 方式注入：h = h * (1 + scale) + shift
/// </summary>
public sealed class UNetResBlock : ITrainableModel
{
    private readonly int _inChannels;
    private readonly int _inH;
    private readonly int _inW;
    private readonly int _outChannels;

    /// <summary>
    ///     创建 U-Net 残差块
    /// </summary>
    /// <param name="inChannels">输入通道数</param>
    /// <param name="outChannels">输出通道数</param>
    /// <param name="timeEmbDim">时间嵌入维度</param>
    /// <param name="numGroups">GroupNorm 分组数</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    public UNetResBlock(int inChannels, int outChannels, int timeEmbDim, int numGroups = 4, int inH = 8, int inW = 8)
    {
        _inChannels = inChannels;
        _outChannels = outChannels;
        _inH = inH;
        _inW = inW;

        Norm1 = new GroupNorm(numGroups, inChannels);
        Conv1 = new Conv2D(inChannels, outChannels, 3, padding: 1, inH: inH, inW: inW);
        TimeProj = new Dense(timeEmbDim, outChannels * 2);
        Norm2 = new GroupNorm(numGroups, outChannels);
        Conv2 = new Conv2D(outChannels, outChannels, 3, padding: 1, inH: inH, inW: inW);

        ShortcutConv = inChannels != outChannels
            ? new Conv2D(inChannels, outChannels, 1, padding: 0, inH: inH, inW: inW)
            : null;
    }

    /// <summary>第一个 GroupNorm</summary>
    public GroupNorm Norm1 { get; }

    /// <summary>第一个卷积</summary>
    public Conv2D Conv1 { get; }

    /// <summary>时间条件投影（输出 2 * outChannels，对应 scale + shift）</summary>
    public Dense TimeProj { get; }

    /// <summary>第二个 GroupNorm</summary>
    public GroupNorm Norm2 { get; }

    /// <summary>第二个卷积</summary>
    public Conv2D Conv2 { get; }

    /// <summary>残差快捷卷积（维度不匹配时使用）</summary>
    public Conv2D? ShortcutConv { get; }

    /// <summary>
    ///     ITrainableModel.Forward（无时间条件，使用零嵌入）
    /// </summary>
    ArrayND ITrainableModel.forward(ArrayND input)
    {
        var batch = input.Shape[0];
        var timeEmbDim = TimeProj.Weight.Shape[0];
        var zeroEmb = ArrayND.Zeros(batch, timeEmbDim);
        return Forward(input, zeroEmb);
    }

    /// <summary>
    ///     ITrainableModel.Forward（带自动微分，使用零嵌入）
    /// </summary>
    ArrayND ITrainableModel.forward(ArrayND input, AutogradContext ctx)
    {
        var batch = input.Shape[0];
        var timeEmbDim = TimeProj.Weight.Shape[0];
        var zeroEmb = ArrayND.Zeros(batch, timeEmbDim);
        return Forward(input, zeroEmb, ctx);
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in Norm1.Parameters()) yield return p;

        foreach (var p in Conv1.Parameters()) yield return p;

        foreach (var p in TimeProj.Parameters()) yield return p;

        foreach (var p in Norm2.Parameters()) yield return p;

        foreach (var p in Conv2.Parameters()) yield return p;

        if (ShortcutConv != null)
            foreach (var p in ShortcutConv.Parameters())
                yield return p;
    }

    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    /// <param name="input">输入张量 [batch, inChannels * h * w]</param>
    /// <param name="timeEmb">时间嵌入 [batch, timeEmbDim]</param>
    /// <returns>输出张量 [batch, outChannels * h * w]</returns>
    public ArrayND Forward(ArrayND input, ArrayND timeEmb)
    {
        var batch = input.Shape[0];

        var norm1Out = Norm1.Forward(Reshape4D(input));
        var silu1 = Activations.SiLUForward(norm1Out);
        var (conv1Out, _, _) = Conv1.Forward(Flatten4D(silu1), _inH, _inW);

        var timeScaleShift = TimeProj.forward(timeEmb);
        var spanTSS = timeScaleShift.AsSpan();
        var conv1Out4D = Reshape4DOut(conv1Out);

        var scaleShiftResult = ApplyScaleShift(conv1Out4D, spanTSS, batch);

        var norm2Out = Norm2.Forward(scaleShiftResult);
        var silu2 = Activations.SiLUForward(norm2Out);
        var (conv2Out, _, _) = Conv2.Forward(Flatten4D(silu2), _inH, _inW);

        var shortcutOut = ShortcutConv != null
            ? ((ITrainableModel)ShortcutConv).forward(input)
            : input;

        var result = AddFlattened(conv2Out, shortcutOut);
        return result;
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="input">输入张量 [batch, inChannels * h * w]</param>
    /// <param name="timeEmb">时间嵌入 [batch, timeEmbDim]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>输出张量 [batch, outChannels * h * w]</returns>
    public ArrayND Forward(ArrayND input, ArrayND timeEmb, AutogradContext ctx)
    {
        var batch = input.Shape[0];

        var norm1Out = Norm1.Forward(Reshape4D(input));
        var silu1 = Activations.SiLUForward(norm1Out);
        var (conv1Out, _, _) = Conv1.Forward(Flatten4D(silu1), _inH, _inW, ctx);

        var timeScaleShift = TimeProj.forward(timeEmb, ctx);
        var conv1Out4D = Reshape4DOut(conv1Out);
        var spanTSS = timeScaleShift.AsSpan();

        var scaleShiftResult = ApplyScaleShift(conv1Out4D, spanTSS, batch);

        var norm2Out = Norm2.Forward(scaleShiftResult);
        var silu2 = Activations.SiLUForward(norm2Out);
        var (conv2Out, _, _) = Conv2.Forward(Flatten4D(silu2), _inH, _inW, ctx);

        var shortcutOut = ShortcutConv != null
            ? ((ITrainableModel)ShortcutConv).forward(input, ctx)
            : input;

        var result = AddFlattenedWithGrad(conv2Out, shortcutOut, ctx);
        return result;
    }

    private ArrayND Reshape4D(ArrayND flat)
    {
        return flat.Reshape(flat.Shape[0], _inChannels, _inH, _inW);
    }

    private ArrayND Reshape4DOut(ArrayND flat)
    {
        return flat.Reshape(flat.Shape[0], _outChannels, _inH, _inW);
    }

    private static ArrayND Flatten4D(ArrayND nd4)
    {
        return nd4.Reshape(nd4.Shape[0], nd4.Shape[1] * nd4.Shape[2] * nd4.Shape[3]);
    }

    private static ArrayND ApplyScaleShift(ArrayND input4D, ReadOnlySpan<float> scaleShift, int batch)
    {
        var c = input4D.Shape[1];
        var h = input4D.Shape[2];
        var w = input4D.Shape[3];
        var result = ArrayND.Zeros(input4D.Shape);
        var spanIn = input4D.AsSpan();
        var spanR = result.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        {
            var scale = scaleShift[n * c * 2];
            var shift = scaleShift[n * c * 2 + 1];
            for (var ch = 0; ch < c; ch++)
            {
                var s = scaleShift[n * c * 2 + ch * 2];
                var sh = scaleShift[n * c * 2 + ch * 2 + 1];
                for (var ih = 0; ih < h; ih++)
                for (var iw = 0; iw < w; iw++)
                {
                    var idx = n * c * h * w + ch * h * w + ih * w + iw;
                    spanR[idx] = spanIn[idx] * (1.0f + s) + sh;
                }
            }
        }

        return result;
    }

    private static ArrayND AddFlattened(ArrayND a, ArrayND b)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] + spanB[i];
        return result;
    }

    private static ArrayND AddFlattenedWithGrad(ArrayND a, ArrayND b, AutogradContext ctx)
    {
        var sum = AddFlattened(a, b);
        ctx.Record(sum, [a, b], outputGrads =>
        {
            var dSum = outputGrads[0];
            return [dSum, dSum];
        });
        return sum;
    }
}
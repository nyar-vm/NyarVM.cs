using Std.DL.Flux;
using Std.DL.Training;

namespace Std.DL.Models;

/// <summary>
///     ResNet-8 精简残差网络 —— 适合小图像分类
///     架构：Conv(3→16) → ResidualBlock×3 → AvgPool → FC(→10)
///     共 8 层带权重的卷积/全连接
/// </summary>
public sealed class ResNet8 : ITrainableModel
{
    /// <summary>
    ///     创建 ResNet-8 模型
    /// </summary>
    /// <param name="numClasses">分类数，默认 10</param>
    public ResNet8(int numClasses = 10)
    {
        InitConv = new Conv2D(3, 16, 3, 1, 1);
        Block1 = new ResidualBlock(16, 16, 1);
        Block2 = new ResidualBlock(16, 32, 2);
        Block3 = new ResidualBlock(32, 64, 2);
        FcOut = new Dense(64, numClasses);
    }

    /// <summary>初始卷积层：3 通道 → 16 通道</summary>
    public Conv2D InitConv { get; }

    /// <summary>残差块 1：16 通道，无下采样</summary>
    public ResidualBlock Block1 { get; }

    /// <summary>残差块 2：16→32 通道，2× 下采样</summary>
    public ResidualBlock Block2 { get; }

    /// <summary>残差块 3：32→64 通道，2× 下采样</summary>
    public ResidualBlock Block3 { get; }

    /// <summary>最终分类全连接层</summary>
    public Dense FcOut { get; }

    /// <summary>
    ///     前向传播（无自动微分）
    /// </summary>
    /// <param name="input">输入 [batch, 3*inH*inW]</param>
    /// <returns>分类 logits [batch, numClasses]</returns>
    public ArrayND forward(ArrayND input)
    {
        var (x, h, w) = InitConv.Forward(input, 32, 32);
        x = Activations.ReLUForward(x);

        (x, h, w) = Block1.Forward(x, h, w, false);
        (x, h, w) = Block2.Forward(x, h, w, false);
        (x, h, w) = Block3.Forward(x, h, w, false);

        var batchSize = x.Shape[0];
        var numChannels = x.Shape[1] / (h * w);
        x = GlobalAvgPool(x, numChannels, h, w);
        x = FcOut.forward(x);

        return x;
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    /// <param name="input">输入 [batch, 3*inH*inW]</param>
    /// <param name="ctx">自动微分上下文</param>
    /// <returns>分类 logits [batch, numClasses]</returns>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var (x, h, w) = InitConv.Forward(input, 32, 32, ctx);
        x = Activations.ReLU(x, ctx);

        (x, h, w) = Block1.Forward(x, h, w, ctx);
        (x, h, w) = Block2.Forward(x, h, w, ctx);
        (x, h, w) = Block3.Forward(x, h, w, ctx);

        var batchSize = x.Shape[0];
        var numChannels = x.Shape[1] / (h * w);
        x = GlobalAvgPool(x, numChannels, h, w);
        x = FcOut.forward(x, ctx);

        return x;
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in InitConv.Parameters()) yield return p;

        foreach (var p in Block1.Parameters()) yield return p;

        foreach (var p in Block2.Parameters()) yield return p;

        foreach (var p in Block3.Parameters()) yield return p;

        foreach (var p in FcOut.Parameters()) yield return p;
    }

    /// <summary>
    ///     全局平均池化：[batch, channels*h*w] → [batch, channels]
    /// </summary>
    private static ArrayND GlobalAvgPool(ArrayND input, int channels, int h, int w)
    {
        var batch = input.Shape[0];
        var result = ArrayND.Zeros(batch, channels);
        var spanInput = input.AsSpan();
        var spanResult = result.AsWriteSpan();

        for (var n = 0; n < batch; n++)
        for (var c = 0; c < channels; c++)
        {
            var sum = 0.0f;
            var baseIdx = n * channels * h * w + c * h * w;
            for (var i = 0; i < h * w; i++) sum += spanInput[baseIdx + i];
            spanResult[n * channels + c] = sum / (h * w);
        }

        return result;
    }
}

/// <summary>
///     ResNet 残差块：Conv → BN → ReLU → Conv → BN → +shortcut → ReLU
/// </summary>
public sealed class ResidualBlock
{
    private readonly int _inChannels;
    private readonly int _outChannels;

    private readonly int _stride;

    /// <summary>
    ///     创建残差块
    /// </summary>
    /// <param name="inChannels">输入通道数</param>
    /// <param name="outChannels">输出通道数</param>
    /// <param name="stride">步幅（>1 时下采样）</param>
    public ResidualBlock(int inChannels, int outChannels, int stride = 1)
    {
        _inChannels = inChannels;
        _outChannels = outChannels;
        _stride = stride;

        ConvA = new Conv2D(inChannels, outChannels, 3, stride, 1);
        BnA = new BatchNorm(outChannels);
        ConvB = new Conv2D(outChannels, outChannels, 3, 1, 1);
        BnB = new BatchNorm(outChannels);

        if (inChannels != outChannels || stride != 1)
        {
            Downsample = new Conv2D(inChannels, outChannels, 1, stride, 0);
            DsBn = new BatchNorm(outChannels);
        }
    }

    /// <summary>第一层卷积</summary>
    public Conv2D ConvA { get; }

    /// <summary>第一层批归一化</summary>
    public BatchNorm BnA { get; }

    /// <summary>第二层卷积</summary>
    public Conv2D ConvB { get; }

    /// <summary>第二层批归一化</summary>
    public BatchNorm BnB { get; }

    /// <summary>下采样卷积（仅当输入/输出通道不一致或 stride>1 时）</summary>
    public Conv2D? Downsample { get; }

    /// <summary>下采样批归一化</summary>
    public BatchNorm? DsBn { get; }

    /// <summary>
    ///     前向传播（可选自动微分）
    /// </summary>
    /// <param name="input">输入张量 [batch, channels*h*w]</param>
    /// <param name="inH">输入高度</param>
    /// <param name="inW">输入宽度</param>
    /// <param name="ctx">自动微分上下文，null 表示不使用 autograd</param>
    /// <returns>(输出, 输出H, 输出W)</returns>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int inH, int inW, AutogradContext? ctx = null)
    {
        ArrayND x;
        int h, w;

        if (ctx is not null)
        {
            (x, h, w) = ConvA.Forward(input, inH, inW, ctx);
            x = ApplySpatialBatchNorm(x, h, w, BnA, ctx);
            x = Activations.ReLU(x, ctx);

            (x, h, w) = ConvB.Forward(x, h, w, ctx);
            x = ApplySpatialBatchNorm(x, h, w, BnB, ctx);

            var shortcut = ComputeShortcut(input, inH, inW, ctx, h, w);
            var (oh, ow) = (h, w);
            x = ElementWiseAdd(x, shortcut);
            x = Activations.ReLU(x, ctx);

            return (x, oh, ow);
        }
        else
        {
            (x, h, w) = ConvA.Forward(input, inH, inW);
            x = ApplySpatialBatchNorm(x, h, w, BnA, null);
            x = Activations.ReLUForward(x);

            (x, h, w) = ConvB.Forward(x, h, w);
            x = ApplySpatialBatchNorm(x, h, w, BnB, null);

            var shortcut = ComputeShortcut(input, inH, inW, null, h, w);
            var (oh, ow) = (h, w);
            x = ElementWiseAdd(x, shortcut);
            x = Activations.ReLUForward(x);

            return (x, oh, ow);
        }
    }

    /// <summary>
    ///     计算跳跃连接的残差（含必要的下采样和 BatchNorm）
    /// </summary>
    private ArrayND ComputeShortcut(ArrayND input, int inH, int inW, AutogradContext? ctx, int outH, int outW)
    {
        if (Downsample is not null && DsBn is not null)
        {
            ArrayND ds;
            if (ctx is not null)
                (ds, _, _) = Downsample.Forward(input, inH, inW, ctx);
            else
                (ds, _, _) = Downsample.Forward(input, inH, inW);

            return ApplySpatialBatchNorm(ds, outH, outW, DsBn, ctx);
        }

        return input;
    }

    /// <summary>
    ///     对卷积输出应用空间批归一化（按通道独立归一化）
    ///     输入形状 [batch, channels*spatial]，按每个通道独立计算均值/方差
    /// </summary>
    private static ArrayND ApplySpatialBatchNorm(
        ArrayND input, int h, int w, BatchNorm bn, AutogradContext? ctx)
    {
        var batch = input.Shape[0];
        var spatial = h * w;
        var channels = input.Shape[1] / spatial;

        if (input.Shape[1] != channels * spatial)
            throw new InvalidOperationException(
                $"SpatialBatchNorm 形状不匹配：input.Shape[1]={input.Shape[1]}, channels={channels}, spatial={spatial}");

        var output = ArrayND.Zeros(input.Shape);
        var spanInput = input.AsSpan();
        var spanOutput = output.AsWriteSpan();
        var spanGamma = bn.Gamma.AsSpan();
        var spanBeta = bn.Beta.AsSpan();
        var eps = 1e-5f;

        for (var c = 0; c < channels; c++)
        {
            var totalElements = batch * spatial;
            var mean = 0.0f;
            var baseOffset = c * spatial;

            for (var n = 0; n < batch; n++)
            for (var s = 0; s < spatial; s++)
                mean += spanInput[n * channels * spatial + baseOffset + s];

            mean /= totalElements;

            var variance = 0.0f;
            for (var n = 0; n < batch; n++)
            for (var s = 0; s < spatial; s++)
            {
                var diff = spanInput[n * channels * spatial + baseOffset + s] - mean;
                variance += diff * diff;
            }

            variance /= totalElements;

            var gamma = spanGamma.Length > c ? spanGamma[System.Math.Min(c, spanGamma.Length - 1)] : 1.0f;
            var beta = spanBeta.Length > c ? spanBeta[System.Math.Min(c, spanBeta.Length - 1)] : 0.0f;
            var invStd = 1.0f / MathF.Sqrt(variance + eps);

            for (var n = 0; n < batch; n++)
            for (var s = 0; s < spatial; s++)
            {
                var idx = n * channels * spatial + baseOffset + s;
                spanOutput[idx] = (spanInput[idx] - mean) * invStd * gamma + beta;
            }
        }

        return output;
    }

    /// <summary>
    ///     前向传播（无 autograd，不使用 ctx）
    /// </summary>
    public (ArrayND Output, int OutH, int OutW) Forward(ArrayND input, int inH, int inW, bool withAutograd)
    {
        return Forward(input, inH, inW, withAutograd ? new AutogradContext() : null);
    }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in ConvA.Parameters()) yield return p;

        foreach (var p in BnA.Parameters()) yield return p;

        foreach (var p in ConvB.Parameters()) yield return p;

        foreach (var p in BnB.Parameters()) yield return p;

        if (Downsample is not null && DsBn is not null)
        {
            foreach (var p in Downsample.Parameters()) yield return p;

            foreach (var p in DsBn.Parameters()) yield return p;
        }
    }

    /// <summary>
    ///     逐元素相加两个张量
    /// </summary>
    private static ArrayND ElementWiseAdd(ArrayND a, ArrayND b)
    {
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();

        if (spanA.Length != spanB.Length)
            throw new InvalidOperationException($"张量形状不兼容：{a.Shape[0]}×{a.Shape[1]} vs {b.Shape[0]}×{b.Shape[1]}");

        var result = ArrayND.Zeros(a.Shape);
        var spanR = result.AsWriteSpan();

        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] + spanB[i];

        return result;
    }
}
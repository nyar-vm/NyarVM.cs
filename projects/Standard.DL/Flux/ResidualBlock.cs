using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     残差块 —— 输出 = 激活(输入 + 子路径(输入))
///     支持 1D（Transformer/MLP）和 2D（ResNet/CNN）残差连接
///     当子路径改变维度时，可选投影快捷方式（projection shortcut）
/// </summary>
public sealed class ResidualBlock : ITrainableModel
{
    private readonly Func<ArrayND, AutogradContext, ArrayND> _activationFn;
    private readonly Func<ArrayND, ArrayND> _activationForwardFn;
    private readonly ITrainableModel _body;
    private readonly ITrainableModel? _shortcut;

    /// <summary>
    ///     创建残差块
    /// </summary>
    /// <param name="body">主路径子网络</param>
    /// <param name="shortcut">可选的投影快捷方式（维度不匹配时使用），null 则恒等映射</param>
    /// <param name="activationFn">带 Autograd 的激活函数，默认 SiLU</param>
    /// <param name="activationForwardFn">纯前向激活函数，默认 SiLUForward</param>
    public ResidualBlock(
        ITrainableModel body,
        ITrainableModel? shortcut = null,
        Func<ArrayND, AutogradContext, ArrayND>? activationFn = null,
        Func<ArrayND, ArrayND>? activationForwardFn = null)
    {
        _body = body;
        _shortcut = shortcut;
        _activationFn = activationFn ?? Activations.SiLU;
        _activationForwardFn = activationForwardFn ?? Activations.SiLUForward;
    }

    /// <summary>
    ///     前向传播（无自动微分）：output = activation(input + body(input))
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        var bodyOut = _body.forward(input);
        var shortcutOut = _shortcut != null ? _shortcut.forward(input) : input;
        var sum = AddArrays(bodyOut, shortcutOut);
        return _activationForwardFn(sum);
    }

    /// <summary>
    ///     前向传播（带自动微分记录）
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var bodyOut = _body.forward(input, ctx);
        var shortcutOut = _shortcut != null ? _shortcut.forward(input, ctx) : input;
        var sum = AddArraysWithGrad(bodyOut, shortcutOut, ctx);
        var output = _activationFn(sum, ctx);

        return output;
    }

    /// <summary>
    ///     获取所有可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        foreach (var p in _body.Parameters()) yield return p;

        if (_shortcut != null)
            foreach (var p in _shortcut.Parameters())
                yield return p;
    }

    private static ArrayND AddArrays(ArrayND a, ArrayND b)
    {
        var result = ArrayND.Zeros(a.Shape);
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var spanR = result.AsWriteSpan();
        for (var i = 0; i < spanR.Length; i++) spanR[i] = spanA[i] + spanB[i];
        return result;
    }

    /// <summary>
    ///     带自动微分的逐元素加法：sum = a + b
    ///     梯度：d_a = d_sum, d_b = d_sum
    /// </summary>
    private static ArrayND AddArraysWithGrad(ArrayND a, ArrayND b, AutogradContext ctx)
    {
        var sum = AddArrays(a, b);

        ctx.Record(sum, [a, b], outputGrads =>
        {
            var dSum = outputGrads[0];
            return [dSum, dSum];
        });

        return sum;
    }
}
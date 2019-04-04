namespace Galatea.Tests;

/// <summary>
///     数值梯度测试基类 —— 使用中心差分法验证 Autograd 梯度正确性
/// </summary>
public abstract class GradientTestBase
{
    /// <summary>有限差分步长</summary>
    protected const float Epsilon = 1e-3f;

    /// <summary>梯度比较容差（有限差分精度约为 O(eps^2)）</summary>
    protected const float GradientTolerance = 1e-3f;

    /// <summary>
    ///     使用固定种子创建随机输入张量
    /// </summary>
    /// <param name="shape">张量形状</param>
    /// <returns>随机张量，值域 [-3, 3]</returns>
    protected static ArrayND RandomInput(params int[] shape)
    {
        var size = shape.Aggregate(1, (a, b) => a * b);
        var data = new float[size];
        var rng = new Random(42);
        for (var i = 0; i < size; i++) data[i] = (rng.NextSingle() * 2.0f - 1.0f) * 3.0f;
        return ArrayND.FromArray(data, shape);
    }

    /// <summary>
    ///     计算两个张量的最大绝对误差
    /// </summary>
    protected static float MaxAbsoluteError(ArrayND a, ArrayND b)
    {
        var spanA = a.AsSpan();
        var spanB = b.AsSpan();
        var maxErr = 0.0f;
        for (var i = 0; i < spanA.Length; i++) maxErr = MathF.Max(maxErr, MathF.Abs(spanA[i] - spanB[i]));
        return maxErr;
    }

    /// <summary>
    ///     使用中心差分法对纯前向函数计算数值梯度
    ///     df/dx[i] ≈ (f(x + h*e_i) - f(x - h*e_i)) / (2*h)
    /// </summary>
    /// <param name="input">输入张量（不会被修改）</param>
    /// <param name="forwardFn">纯前向函数：接收 ArrayND，返回标量</param>
    /// <returns>数值梯度张量，与 input 同形状</returns>
    protected static ArrayND NumericalGradient(ArrayND input, Func<ArrayND, float> forwardFn)
    {
        var grad = ArrayND.Zeros(input.Shape);
        var spanGrad = grad.AsWriteSpan();

        var temp = input.Clone();
        var spanTemp = temp.AsWriteSpan();

        for (var i = 0; i < spanTemp.Length; i++)
        {
            var orig = spanTemp[i];

            spanTemp[i] = orig + Epsilon;
            var lossPlus = forwardFn(temp);

            spanTemp[i] = orig - Epsilon;
            var lossMinus = forwardFn(temp);

            spanTemp[i] = orig;
            spanGrad[i] = (lossPlus - lossMinus) / (2.0f * Epsilon);
        }

        return grad;
    }

    /// <summary>
    ///     通过 Autograd 计算某个张量相对于标量损失的梯度
    /// </summary>
    /// <param name="input">输入张量（会被 Autograd 追踪梯度）</param>
    /// <param name="autogradFn">带 AutogradContext 的前向函数：返回输出张量</param>
    /// <param name="lossFn">将输出转换为用于 Backward 的损失张量</param>
    /// <returns>input.Grad（Autograd 计算的梯度）</returns>
    protected static ArrayND AutogradGradient(
        ArrayND input,
        Func<ArrayND, AutogradContext, ArrayND> autogradFn,
        Func<ArrayND, ArrayND> lossFn)
    {
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var output = autogradFn(input, ctx);
        var loss = lossFn(output);

        ctx.Backward(loss);

        return input.Grad!;
    }

    /// <summary>
    ///     验证 Autograd 梯度与数值梯度的一致性
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <param name="autogradFn">带 AutogradContext 的前向函数</param>
    /// <param name="pureForwardFn">纯前向函数（无 ctx），用于数值梯度</param>
    /// <param name="lossFn">将前向输出转换为标量用于 Backward</param>
    /// <returns>Autograd 梯度与数值梯度的最大绝对误差</returns>
    protected float VerifyGradient(
        ArrayND input,
        Func<ArrayND, AutogradContext, ArrayND> autogradFn,
        Func<ArrayND, float> pureForwardFn,
        Func<ArrayND, ArrayND> lossFn)
    {
        var autoGrad = AutogradGradient(input.Clone(), autogradFn, lossFn);
        var numGrad = NumericalGradient(input.Clone(), pureForwardFn);

        return MaxAbsoluteError(autoGrad, numGrad);
    }

    /// <summary>
    ///     对激活函数执行梯度验证
    ///     注意：AutogradContext.Backward 仅对 loss.Grad[0] = 1.0f 播种梯度，
    ///     因此数值梯度和 Autograd 梯度都相对于输出[0]
    /// </summary>
    /// <param name="activFn">带 Autograd 的激活函数 (input, ctx) => output</param>
    /// <param name="pureFn">纯前向激活函数 input => output（不带 ctx）</param>
    /// <param name="shape">测试输入形状</param>
    /// <returns>最大梯度误差</returns>
    protected float VerifyActivationGradient(
        Func<ArrayND, AutogradContext, ArrayND> activFn,
        Func<ArrayND, ArrayND> pureFn,
        params int[] shape)
    {
        var input = RandomInput(shape);

        return VerifyGradient(
            input,
            activFn,
            x => pureFn(x).AsSpan()[0],
            output => output
        );
    }
}
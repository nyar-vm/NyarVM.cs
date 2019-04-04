namespace Std.DL.Flux;

/// <summary>
///     展平操作
/// </summary>
public static class Flatten
{
    /// <summary>
    ///     将空间特征展平为一维向量
    /// </summary>
    /// <param name="input">输入张量 [batch, channels * h * w]</param>
    /// <param name="channels">通道数</param>
    /// <param name="h">高度</param>
    /// <param name="w">宽度</param>
    /// <returns>展平后的张量 [batch, channels * h * w]</returns>
    public static ArrayND Forward(ArrayND input, int channels, int h, int w)
    {
        return input.Reshape(input.Shape[0], channels * h * w);
    }

    /// <summary>
    ///     展平操作（带自动微分记录）
    /// </summary>
    public static ArrayND Forward(ArrayND input, int channels, int h, int w, AutogradContext ctx)
    {
        var output = input.Reshape(input.Shape[0], channels * h * w);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = dOutput.Reshape(input.Shape);
            return [dInput];
        });

        return output;
    }
}
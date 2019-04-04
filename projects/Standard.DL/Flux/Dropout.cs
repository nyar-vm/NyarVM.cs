namespace Std.DL.Flux;

/// <summary>
///     Dropout 正则化算子
/// </summary>
public class Dropout
{
    private readonly float _rate;
    private readonly Random _rng;

    /// <summary>
    ///     创建 Dropout 层
    /// </summary>
    /// <param name="rate">丢弃概率</param>
    /// <param name="seed">随机种子，null 时使用 Random.Shared</param>
    public Dropout(float rate = 0.5f, int? seed = 42)
    {
        _rate = rate;
        _rng = seed.HasValue ? new Random(seed.Value) : Random.Shared;
    }

    /// <summary>
    ///     是否为训练模式
    /// </summary>
    public bool IsTraining { get; set; } = true;

    /// <summary>
    ///     Dropout 前向
    /// </summary>
    /// <param name="input">输入张量</param>
    /// <returns>训练模式：随机置零后的输出，保留的神经元缩放 1/(1-rate)；评估模式：恒等映射</returns>
    public ArrayND Forward(ArrayND input)
    {
        if (!IsTraining) return input.Clone();

        var scale = 1.0f / (1.0f - _rate);
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();

        for (var i = 0; i < spanIn.Length; i++)
            if (_rng.NextSingle() > _rate)
                spanOut[i] = spanIn[i] * scale;

        return result;
    }

    /// <summary>
    ///     Dropout 前向（带自动微分）
    /// </summary>
    public ArrayND Forward(ArrayND input, AutogradContext ctx)
    {
        if (!IsTraining)
        {
            ctx.Record(input.Clone(), [input], outputGrads => [outputGrads[0]]);
            return input.Clone();
        }

        var output = Forward(input);

        ctx.Record(output, [input], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = ArrayND.Zeros(input.Shape);
            var spanOut = output.AsSpan();
            var spanUp = dOutput.AsSpan();
            var spanR = dInput.AsWriteSpan();
            var scale = 1.0f / (1.0f - _rate);

            for (var i = 0; i < spanOut.Length; i++)
                if (spanOut[i] != 0.0f)
                    spanR[i] = spanUp[i] * scale;

            return [dInput];
        });

        return output;
    }
}
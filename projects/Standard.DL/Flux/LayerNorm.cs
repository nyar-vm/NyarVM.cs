namespace Std.DL.Flux;

/// <summary>
///     Layer Normalization —— Transformer 标准归一化
/// </summary>
public class LayerNorm : ILayer
{
    internal readonly float _epsilon;
    private readonly int _normalizedShape;

    /// <summary>
    ///     创建 LayerNorm 层
    /// </summary>
    /// <param name="normalizedShape">归一化维度</param>
    /// <param name="epsilon">数值稳定项</param>
    public LayerNorm(int normalizedShape, float epsilon = 1e-5f)
    {
        _normalizedShape = normalizedShape;
        _epsilon = epsilon;
        Gamma = ArrayND.FromArray([.. Enumerable.Repeat(1.0f, normalizedShape)], normalizedShape);
        Beta = ArrayND.Zeros(normalizedShape);
    }

    /// <summary>
    ///     缩放参数 gamma
    /// </summary>
    public ArrayND Gamma { get; }

    /// <summary>
    ///     偏移参数 beta
    /// </summary>
    public ArrayND Beta { get; }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        yield return new Parameter(Gamma);
        yield return new Parameter(Beta);
    }

    /// <summary>
    ///     前向传播
    /// </summary>
    public ArrayND Forward(ArrayND input)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();
        var spanG = Gamma.AsSpan();
        var spanB = Beta.AsSpan();

        var totalElements = input.Size;
        var groupCount = totalElements / _normalizedShape;

        for (var g = 0; g < groupCount; g++)
        {
            var off = g * _normalizedShape;
            var sum = 0.0f;
            for (var i = 0; i < _normalizedShape; i++) sum += spanIn[off + i];
            var mean = sum / _normalizedShape;

            var varSum = 0.0f;
            for (var i = 0; i < _normalizedShape; i++)
            {
                var diff = spanIn[off + i] - mean;
                varSum += diff * diff;
            }

            var variance = varSum / _normalizedShape;
            var invStd = 1.0f / MathF.Sqrt(variance + _epsilon);

            for (var i = 0; i < _normalizedShape; i++)
            {
                var normalized = (spanIn[off + i] - mean) * invStd;
                spanOut[off + i] = spanG[i] * normalized + spanB[i];
            }
        }

        return result;
    }

    /// <summary>
    ///     带自动微分的前向
    /// </summary>
    public ArrayND Forward(ArrayND input, AutogradContext ctx)
    {
        var output = Forward(input);

        ctx.Record(output, [input, Gamma, Beta], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = ArrayND.Zeros(input.Shape);
            var dGamma = ArrayND.Zeros(Gamma.Shape);
            var dBeta = ArrayND.Zeros(Beta.Shape);

            var spanIn = input.AsSpan();
            var spanDOut = dOutput.AsSpan();
            var spanG = Gamma.AsSpan();
            var spanDIn = dInput.AsWriteSpan();
            var spanDG = dGamma.AsWriteSpan();
            var spanDB = dBeta.AsWriteSpan();

            var totalElements = input.Size;
            var groupCount = totalElements / _normalizedShape;

            for (var g = 0; g < groupCount; g++)
            {
                var off = g * _normalizedShape;

                var sumIn = 0.0f;
                var sumSq = 0.0f;
                for (var i = 0; i < _normalizedShape; i++)
                {
                    sumIn += spanIn[off + i];
                    sumSq += spanIn[off + i] * spanIn[off + i];
                }

                var mean = sumIn / _normalizedShape;
                var variance = sumSq / _normalizedShape - mean * mean;
                var invStd = 1.0f / MathF.Sqrt(variance + _epsilon);

                var sumDxNorm = 0.0f;
                var sumDxNormXCentered = 0.0f;

                for (var i = 0; i < _normalizedShape; i++)
                {
                    var dy = spanDOut[off + i];
                    spanDB[i] += dy;

                    var xCentered = spanIn[off + i] - mean;
                    var xNorm = xCentered * invStd;
                    spanDG[i] += dy * xNorm;

                    var dxNorm = dy * spanG[i];
                    sumDxNorm += dxNorm;
                    sumDxNormXCentered += dxNorm * xCentered;
                }

                var dVar = sumDxNormXCentered * -0.5f * invStd * invStd * invStd;
                var dMean = sumDxNorm * -invStd;

                for (var i = 0; i < _normalizedShape; i++)
                {
                    var xCentered = spanIn[off + i] - mean;
                    var dxNorm = spanDOut[off + i] * spanG[i];
                    spanDIn[off + i] = dxNorm * invStd + dVar * 2.0f * xCentered / _normalizedShape +
                                       dMean / _normalizedShape;
                }
            }

            return [dInput, dGamma, dBeta];
        });

        return output;
    }
}
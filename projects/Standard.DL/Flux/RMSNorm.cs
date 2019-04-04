using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     RMS Normalization —— LLaMA / Mistral 标准归一化
///     y = x * gamma / RMS(x), 其中 RMS(x) = sqrt(mean(x^2) + epsilon)
/// </summary>
public class RMSNorm : ILayer, ITrainableModel
{
    internal readonly float _epsilon;
    private readonly int _normalizedShape;

    /// <summary>
    ///     创建 RMSNorm 层
    /// </summary>
    /// <param name="normalizedShape">归一化维度</param>
    /// <param name="epsilon">数值稳定项</param>
    public RMSNorm(int normalizedShape, float epsilon = 1e-5f)
    {
        _normalizedShape = normalizedShape;
        _epsilon = epsilon;
        Gamma = ArrayND.FromArray([.. Enumerable.Repeat(1.0f, normalizedShape)], normalizedShape);
    }

    /// <summary>
    ///     缩放参数 gamma
    /// </summary>
    public ArrayND Gamma { get; }

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        yield return new Parameter(Gamma);
    }

    /// <summary>
    ///     前向传播
    /// </summary>
    public ArrayND forward(ArrayND input)
    {
        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();
        var spanG = Gamma.AsSpan();

        var totalElements = input.Size;
        var groupCount = totalElements / _normalizedShape;

        for (var g = 0; g < groupCount; g++)
        {
            var off = g * _normalizedShape;
            var sumSq = 0.0f;
            for (var i = 0; i < _normalizedShape; i++) sumSq += spanIn[off + i] * spanIn[off + i];
            var rms = MathF.Sqrt(sumSq / _normalizedShape + _epsilon);
            var invRms = 1.0f / rms;

            for (var i = 0; i < _normalizedShape; i++) spanOut[off + i] = spanG[i] * spanIn[off + i] * invRms;
        }

        return result;
    }

    /// <summary>
    ///     带自动微分的前向
    /// </summary>
    public ArrayND forward(ArrayND input, AutogradContext ctx)
    {
        var output = forward(input);

        ctx.Record(output, [input, Gamma], outputGrads =>
        {
            var dOutput = outputGrads[0];
            var dInput = ArrayND.Zeros(input.Shape);
            var dGamma = ArrayND.Zeros(Gamma.Shape);

            var spanIn = input.AsSpan();
            var spanDOut = dOutput.AsSpan();
            var spanG = Gamma.AsSpan();
            var spanDIn = dInput.AsWriteSpan();
            var spanDG = dGamma.AsWriteSpan();

            var totalElements = input.Size;
            var groupCount = totalElements / _normalizedShape;

            for (var g = 0; g < groupCount; g++)
            {
                var off = g * _normalizedShape;

                var sumSq = 0.0f;
                for (var i = 0; i < _normalizedShape; i++) sumSq += spanIn[off + i] * spanIn[off + i];
                var rms = MathF.Sqrt(sumSq / _normalizedShape + _epsilon);
                var invRms = 1.0f / rms;
                var invRms3 = invRms * invRms * invRms;

                var dv = 0.0f;
                for (var i = 0; i < _normalizedShape; i++)
                {
                    spanDG[i] += spanDOut[off + i] * spanIn[off + i] * invRms;
                    dv += spanDOut[off + i] * spanG[i] * spanIn[off + i];
                }

                for (var i = 0; i < _normalizedShape; i++)
                {
                    var dy = spanDOut[off + i];
                    var xv = spanIn[off + i];
                    var gi = spanG[i];
                    spanDIn[off + i] = gi * dy * invRms - xv * dv / (_normalizedShape * rms * rms * rms);
                }
            }

            return [dInput, dGamma];
        });

        return output;
    }
}
namespace Std.DL.Flux;

/// <summary>
///     Batch Normalization 算子
/// </summary>
public class BatchNorm : ILayer
{
    internal readonly float _epsilon;

    internal readonly int _numFeatures;
    internal readonly float[] _runningMean;
    internal readonly float[] _runningVar;
    private int _batchCount;

    /// <summary>
    ///     创建 BatchNorm 层
    /// </summary>
    /// <param name="numFeatures">特征数</param>
    /// <param name="epsilon">数值稳定项</param>
    public BatchNorm(int numFeatures, float epsilon = 1e-5f)
    {
        _numFeatures = numFeatures;
        Gamma = ArrayND.FromArray([.. Enumerable.Repeat(1.0f, numFeatures)], 1, numFeatures);
        Beta = ArrayND.Zeros(1, numFeatures);
        _epsilon = epsilon;
        _runningMean = new float[numFeatures];
        _runningVar = new float[numFeatures];
        for (var i = 0; i < numFeatures; i++) _runningVar[i] = 1.0f;
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
    ///     EMA 动量
    /// </summary>
    public float Momentum { get; set; } = 0.9f;

    /// <summary>
    ///     是否为训练模式
    /// </summary>
    public bool IsTraining { get; set; } = true;

    /// <summary>
    ///     获取可训练参数
    /// </summary>
    public IEnumerable<IParameter> Parameters()
    {
        yield return new Parameter(Gamma);
        yield return new Parameter(Beta);
    }

    /// <summary>
    ///     BatchNorm 前向
    /// </summary>
    /// <param name="input">输入 [batch, features]</param>
    /// <returns>归一化输出</returns>
    public ArrayND Forward(ArrayND input)
    {
        var batch = input.Shape[0];
        var features = input.Shape[1];
        var result = ArrayND.Zeros(batch, features);

        if (IsTraining)
        {
            _batchCount++;
            for (var f = 0; f < features; f++)
            {
                var sum = 0.0f;
                for (var b = 0; b < batch; b++) sum += input.AsSpan()[b * features + f];
                var mean = sum / batch;

                var varSum = 0.0f;
                for (var b = 0; b < batch; b++)
                {
                    var diff = input.AsSpan()[b * features + f] - mean;
                    varSum += diff * diff;
                }

                var variance = varSum / batch;

                _runningMean[f] = Momentum * _runningMean[f] + (1.0f - Momentum) * mean;
                _runningVar[f] = Momentum * _runningVar[f] + (1.0f - Momentum) * variance;

                var gammaVal = Gamma.AsSpan()[f];
                var betaVal = Beta.AsSpan()[f];
                var invStd = 1.0f / MathF.Sqrt(variance + _epsilon);

                for (var b = 0; b < batch; b++)
                {
                    var normalized = (input.AsSpan()[b * features + f] - mean) * invStd;
                    result.AsWriteSpan()[b * features + f] = gammaVal * normalized + betaVal;
                }
            }
        }
        else
        {
            for (var f = 0; f < features; f++)
            {
                var gammaVal = Gamma.AsSpan()[f];
                var betaVal = Beta.AsSpan()[f];
                var invStd = 1.0f / MathF.Sqrt(_runningVar[f] + _epsilon);

                for (var b = 0; b < batch; b++)
                {
                    var normalized = (input.AsSpan()[b * features + f] - _runningMean[f]) * invStd;
                    result.AsWriteSpan()[b * features + f] = gammaVal * normalized + betaVal;
                }
            }
        }

        return result;
    }

    /// <summary>
    ///     BatchNorm 前向（带自动微分）
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

            var batch = input.Shape[0];
            var features = input.Shape[1];
            var invBatch = 1.0f / batch;

            for (var f = 0; f < features; f++)
            {
                var sumX = 0.0f;
                var xHat = new float[batch];
                for (var b = 0; b < batch; b++) sumX += input.AsSpan()[b * features + f];
                var mean = sumX * invBatch;

                var sumVar = 0.0f;
                for (var b = 0; b < batch; b++)
                {
                    xHat[b] = input.AsSpan()[b * features + f] - mean;
                    sumVar += xHat[b] * xHat[b];
                }

                var variance = sumVar * invBatch;
                var invStd = 1.0f / MathF.Sqrt(variance + _epsilon);

                var gammaVal = Gamma.AsSpan()[f];
                var dGammaSum = 0.0f;
                var dBetaSum = 0.0f;
                var sumDxHatXHat = 0.0f;
                var sumDxHat = 0.0f;

                for (var b = 0; b < batch; b++)
                {
                    var dy = dOutput.AsSpan()[b * features + f];
                    dBetaSum += dy;
                    dGammaSum += dy * xHat[b] * invStd;

                    var dxHat = dy * gammaVal;
                    sumDxHatXHat += dxHat * xHat[b];
                    sumDxHat += dxHat;
                }

                dBeta.AsWriteSpan()[f] = dBetaSum;
                dGamma.AsWriteSpan()[f] = dGammaSum;

                var dVar = sumDxHatXHat * -0.5f * invStd * invStd * invStd;
                var dMean = sumDxHat * -invStd;

                for (var b = 0; b < batch; b++)
                {
                    var dy = dOutput.AsSpan()[b * features + f];
                    var dxHat = dy * gammaVal;
                    dInput.AsWriteSpan()[b * features + f] =
                        dxHat * invStd + dVar * 2.0f * xHat[b] * invBatch + dMean * invBatch;
                }
            }

            return [dInput, dGamma, dBeta];
        });

        return output;
    }
}
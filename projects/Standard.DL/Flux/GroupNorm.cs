namespace Std.DL.Flux;

/// <summary>
///     Group Normalization —— Stable Diffusion U-Net 标准归一化
///     将通道分组后按组归一化，对小 batch 友好
/// </summary>
public class GroupNorm : ILayer
{
    private readonly float _epsilon;
    private readonly int _numChannels;
    private readonly int _numGroups;

    /// <summary>
    ///     创建 GroupNorm 层
    /// </summary>
    /// <param name="numGroups">分组数（SD 常用 32）</param>
    /// <param name="numChannels">通道数</param>
    /// <param name="epsilon">数值稳定项</param>
    public GroupNorm(int numGroups, int numChannels, float epsilon = 1e-5f)
    {
        _numGroups = numGroups;
        _numChannels = numChannels;
        _epsilon = epsilon;
        Gamma = ArrayND.FromArray([.. Enumerable.Repeat(1.0f, numChannels)], numChannels);
        Beta = ArrayND.Zeros(numChannels);
    }

    /// <summary>
    ///     缩放参数 gamma [numChannels]
    /// </summary>
    public ArrayND Gamma { get; }

    /// <summary>
    ///     偏移参数 beta [numChannels]
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
    /// <param name="input">输入 [N, C, H, W]</param>
    public ArrayND Forward(ArrayND input)
    {
        var n = input.Shape[0];
        var c = input.Shape[1];
        var h = input.Shape[2];
        var w = input.Shape[3];
        var channelsPerGroup = c / _numGroups;
        var spatialSize = h * w;
        var groupSize = channelsPerGroup * spatialSize;

        var result = ArrayND.Zeros(input.Shape);
        var spanIn = input.AsSpan();
        var spanOut = result.AsWriteSpan();
        var spanG = Gamma.AsSpan();
        var spanB = Beta.AsSpan();

        for (var batchIdx = 0; batchIdx < n; batchIdx++)
        for (var g = 0; g < _numGroups; g++)
        {
            var cStart = g * channelsPerGroup;
            var sum = 0.0f;
            var sumSq = 0.0f;

            for (var ci = 0; ci < channelsPerGroup; ci++)
            for (var sp = 0; sp < spatialSize; sp++)
            {
                var idx = batchIdx * c * spatialSize + (cStart + ci) * spatialSize + sp;
                var val = spanIn[idx];
                sum += val;
                sumSq += val * val;
            }

            var mean = sum / groupSize;
            var variance = sumSq / groupSize - mean * mean;
            var invStd = 1.0f / MathF.Sqrt(variance + _epsilon);

            for (var ci = 0; ci < channelsPerGroup; ci++)
            {
                var ch = cStart + ci;
                for (var sp = 0; sp < spatialSize; sp++)
                {
                    var idx = batchIdx * c * spatialSize + ch * spatialSize + sp;
                    spanOut[idx] = spanG[ch] * (spanIn[idx] - mean) * invStd + spanB[ch];
                }
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

            var n = input.Shape[0];
            var c = input.Shape[1];
            var h = input.Shape[2];
            var w = input.Shape[3];
            var channelsPerGroup = c / _numGroups;
            var spatialSize = h * w;
            var groupSize = channelsPerGroup * spatialSize;

            var spanIn = input.AsSpan();
            var spanDOut = dOutput.AsSpan();
            var spanG = Gamma.AsSpan();
            var spanDIn = dInput.AsWriteSpan();
            var spanDG = dGamma.AsWriteSpan();
            var spanDB = dBeta.AsWriteSpan();

            for (var batchIdx = 0; batchIdx < n; batchIdx++)
            for (var g = 0; g < _numGroups; g++)
            {
                var cStart = g * channelsPerGroup;

                var sum = 0.0f;
                var sumSq = 0.0f;
                for (var ci = 0; ci < channelsPerGroup; ci++)
                for (var sp = 0; sp < spatialSize; sp++)
                {
                    var idx = batchIdx * c * spatialSize + (cStart + ci) * spatialSize + sp;
                    var val = spanIn[idx];
                    sum += val;
                    sumSq += val * val;
                }

                var mean = sum / groupSize;
                var variance = sumSq / groupSize - mean * mean;
                var invStd = 1.0f / MathF.Sqrt(variance + _epsilon);

                var sumDxNorm = 0.0f;
                var sumDxNormXCentered = 0.0f;

                for (var ci = 0; ci < channelsPerGroup; ci++)
                {
                    var ch = cStart + ci;
                    for (var sp = 0; sp < spatialSize; sp++)
                    {
                        var idx = batchIdx * c * spatialSize + ch * spatialSize + sp;
                        var dy = spanDOut[idx];
                        var xCentered = spanIn[idx] - mean;
                        spanDB[ch] += dy;

                        var xNorm = xCentered * invStd;
                        spanDG[ch] += dy * xNorm;

                        var dxNorm = dy * spanG[ch];
                        sumDxNorm += dxNorm;
                        sumDxNormXCentered += dxNorm * xCentered;
                    }
                }

                var dVar = sumDxNormXCentered * -0.5f * invStd * invStd * invStd;
                var dMean = sumDxNorm * -invStd;

                for (var ci = 0; ci < channelsPerGroup; ci++)
                {
                    var ch = cStart + ci;
                    for (var sp = 0; sp < spatialSize; sp++)
                    {
                        var idx = batchIdx * c * spatialSize + ch * spatialSize + sp;
                        var dy = spanDOut[idx];
                        var xCentered = spanIn[idx] - mean;
                        var dxNorm = dy * spanG[ch];
                        spanDIn[idx] = dxNorm * invStd + dVar * 2.0f * xCentered / groupSize + dMean / groupSize;
                    }
                }
            }

            return [dInput, dGamma, dBeta];
        });

        return output;
    }
}
using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     学习率查找器 —— 自动选择最优学习率
///     从极小学习率开始，逐步增大，记录每个学习率下的损失
///     最优学习率通常在损失下降最快的位置
/// </summary>
public sealed class LRFinder
{
    private readonly float _endLR;
    private readonly Func<ITrainableModel, ArrayND, AutogradContext, ArrayND> _forwardFn;
    private readonly Func<ArrayND, ArrayND, AutogradContext, ArrayND> _lossFn;
    private readonly float _maxGradNorm;
    private readonly ITrainableModel _model;
    private readonly int _numSteps;
    private readonly IOptimizer _optimizer;
    private readonly float _startLR;

    /// <summary>
    ///     创建学习率查找器
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="optimizer">优化器</param>
    /// <param name="forwardFn">前向函数</param>
    /// <param name="lossFn">损失函数</param>
    /// <param name="startLR">起始学习率</param>
    /// <param name="endLR">结束学习率</param>
    /// <param name="numSteps">测试步数</param>
    /// <param name="maxGradNorm">梯度裁剪阈值</param>
    public LRFinder(
        ITrainableModel model,
        IOptimizer optimizer,
        Func<ITrainableModel, ArrayND, AutogradContext, ArrayND>? forwardFn = null,
        Func<ArrayND, ArrayND, AutogradContext, ArrayND>? lossFn = null,
        float startLR = 1e-7f,
        float endLR = 10.0f,
        int numSteps = 100,
        float maxGradNorm = 1.0f)
    {
        _model = model;
        _optimizer = optimizer;
        _forwardFn = forwardFn ?? DefaultForward;
        _lossFn = lossFn ?? DefaultLoss;
        _startLR = startLR;
        _endLR = endLR;
        _numSteps = numSteps;
        _maxGradNorm = maxGradNorm;
    }

    /// <summary>
    ///     运行学习率查找
    /// </summary>
    /// <param name="inputs">训练输入</param>
    /// <param name="targets">训练目标</param>
    /// <returns>学习率-损失曲线</returns>
    public LRFinderResult Run(ArrayND inputs, ArrayND targets)
    {
        var originalParams = SaveParameters();

        var lrMultiplier = MathF.Pow(_endLR / _startLR, 1.0f / _numSteps);

        var learningRates = new List<float>(_numSteps);
        var losses = new List<float>(_numSteps);
        var bestLoss = float.MaxValue;
        var currentLR = _startLR;

        for (var step = 0; step < _numSteps; step++)
        {
            SetLearningRate(currentLR);

            var ctx = new AutogradContext();
            ctx.StartRecording();

            var logits = _forwardFn(_model, inputs, ctx);
            var loss = _lossFn(logits, targets, ctx);
            ctx.Backward(loss);

            var lossVal = loss.AsSpan()[0];

            if (_maxGradNorm > 0)
            {
                var allParams = _model.Parameters().ToList();
                GradientClipping.ClipGradNorm(allParams, _maxGradNorm);
            }

            var paramList = _model.Parameters().ToList();
            _optimizer.Step(paramList);
            _optimizer.ZeroGrad(paramList);

            learningRates.Add(currentLR);
            losses.Add(lossVal);

            if (lossVal < bestLoss) bestLoss = lossVal;

            if (lossVal > bestLoss * 4.0f) break;

            currentLR *= lrMultiplier;
        }

        RestoreParameters(originalParams);

        return new LRFinderResult(learningRates, losses);
    }

    /// <summary>
    ///     建议最优学习率（损失下降最快的位置）
    /// </summary>
    /// <param name="result">学习率查找结果</param>
    /// <returns>建议学习率</returns>
    public static float SuggestLR(LRFinderResult result)
    {
        if (result.Losses.Count < 3) return result.LearningRates[0];

        var maxSlope = float.NegativeInfinity;
        var bestIdx = 0;

        for (var i = 2; i < result.Losses.Count; i++)
        {
            var slope = (result.Losses[i - 2] - result.Losses[i])
                        / (MathF.Log10(result.LearningRates[i]) - MathF.Log10(result.LearningRates[i - 2]));
            if (slope > maxSlope)
            {
                maxSlope = slope;
                bestIdx = i - 1;
            }
        }

        return result.LearningRates[bestIdx];
    }

    /// <summary>
    ///     建议最优学习率（最小损失对应的学习率的 1/2）
    /// </summary>
    /// <param name="result">学习率查找结果</param>
    /// <returns>建议学习率</returns>
    public static float SuggestLRConservative(LRFinderResult result)
    {
        if (result.Losses.Count == 0) return 0.001f;

        var minLoss = float.MaxValue;
        var minIdx = 0;
        for (var i = 0; i < result.Losses.Count; i++)
            if (result.Losses[i] < minLoss)
            {
                minLoss = result.Losses[i];
                minIdx = i;
            }

        var halfIdx = minIdx / 2;
        return result.LearningRates[System.Math.Max(0, halfIdx)];
    }

    private Dictionary<string, ArrayND> SaveParameters()
    {
        var saved = new Dictionary<string, ArrayND>();
        var idx = 0;
        foreach (var p in _model.Parameters())
        {
            var data = ArrayND.Zeros(p.Value.Shape);
            var spanSrc = p.Value.AsSpan();
            var spanDst = data.AsWriteSpan();
            for (var i = 0; i < spanSrc.Length; i++) spanDst[i] = spanSrc[i];
            saved[$"p{idx}"] = data;
            idx++;
        }

        return saved;
    }

    private void RestoreParameters(Dictionary<string, ArrayND> saved)
    {
        var idx = 0;
        foreach (var p in _model.Parameters())
        {
            if (saved.TryGetValue($"p{idx}", out var data))
            {
                var spanSrc = data.AsSpan();
                var spanDst = p.Value.AsWriteSpan();
                for (var i = 0; i < spanSrc.Length && i < spanDst.Length; i++) spanDst[i] = spanSrc[i];
            }

            idx++;
        }
    }

    private void SetLearningRate(float lr)
    {
        var paramList = _model.Parameters().ToList();
        _optimizer.ZeroGrad(paramList);
    }

    private static ArrayND DefaultForward(ITrainableModel model, ArrayND inputs, AutogradContext ctx)
    {
        if (model is GPTModel gpt) return gpt.ForwardForTraining(inputs, ctx);

        if (model is WeightTiedGPTModel wt) return wt.ForwardForTraining(inputs, ctx);

        return model.forward(inputs, ctx);
    }

    private static ArrayND DefaultLoss(ArrayND logits, ArrayND targets, AutogradContext ctx)
    {
        var (loss, _) = Losses.SoftmaxCrossEntropy(logits, targets, ctx);
        return loss;
    }
}

/// <summary>
///     学习率查找结果
/// </summary>
public sealed class LRFinderResult
{
    /// <summary>
    ///     创建学习率查找结果
    /// </summary>
    /// <param name="learningRates">学习率列表</param>
    /// <param name="losses">损失列表</param>
    public LRFinderResult(List<float> learningRates, List<float> losses)
    {
        LearningRates = learningRates;
        Losses = losses;
    }

    /// <summary>
    ///     学习率列表
    /// </summary>
    public IReadOnlyList<float> LearningRates { get; }

    /// <summary>
    ///     损失列表
    /// </summary>
    public IReadOnlyList<float> Losses { get; }
}
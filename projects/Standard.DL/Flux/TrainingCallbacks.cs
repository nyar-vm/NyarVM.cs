using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     训练回调接口 —— 在训练循环的关键节点触发自定义逻辑
/// </summary>
public interface ITrainingCallback
{
    /// <summary>
    ///     训练开始时调用
    /// </summary>
    /// <param name="state">训练状态</param>
    void OnTrainBegin(TrainingCallbackState state)
    {
    }

    /// <summary>
    ///     训练结束时调用
    /// </summary>
    /// <param name="state">训练状态</param>
    void OnTrainEnd(TrainingCallbackState state)
    {
    }

    /// <summary>
    ///     每个 epoch 开始时调用
    /// </summary>
    /// <param name="state">训练状态</param>
    void OnEpochBegin(TrainingCallbackState state)
    {
    }

    /// <summary>
    ///     每个 epoch 结束时调用
    /// </summary>
    /// <param name="state">训练状态</param>
    void OnEpochEnd(TrainingCallbackState state)
    {
    }

    /// <summary>
    ///     每个训练步骤后调用
    /// </summary>
    /// <param name="state">训练状态</param>
    void OnTrainStep(TrainingCallbackState state)
    {
    }

    /// <summary>
    ///     评估完成后调用
    /// </summary>
    /// <param name="state">训练状态</param>
    void OnEvaluate(TrainingCallbackState state)
    {
    }
}

/// <summary>
///     训练回调状态 —— 传递给回调的上下文信息
/// </summary>
public sealed class TrainingCallbackState
{
    /// <summary>
    ///     创建训练回调状态
    /// </summary>
    /// <param name="epoch">当前 epoch</param>
    /// <param name="globalStep">全局步数</param>
    /// <param name="model">模型</param>
    /// <param name="optimizer">优化器</param>
    public TrainingCallbackState(int epoch, int globalStep, ITrainableModel model, IOptimizer optimizer)
    {
        Epoch = epoch;
        GlobalStep = globalStep;
        Model = model;
        Optimizer = optimizer;
    }

    /// <summary>
    ///     当前 epoch
    /// </summary>
    public int Epoch { get; set; }

    /// <summary>
    ///     全局步数
    /// </summary>
    public int GlobalStep { get; set; }

    /// <summary>
    ///     当前训练损失
    /// </summary>
    public float TrainLoss { get; set; }

    /// <summary>
    ///     当前评估损失
    /// </summary>
    public float EvalLoss { get; set; }

    /// <summary>
    ///     当前评估困惑度
    /// </summary>
    public float EvalPerplexity { get; set; }

    /// <summary>
    ///     当前学习率
    /// </summary>
    public float LearningRate { get; set; }

    /// <summary>
    ///     是否请求停止训练
    /// </summary>
    public bool StopRequested { get; set; }

    /// <summary>
    ///     模型引用
    /// </summary>
    public ITrainableModel Model { get; set; }

    /// <summary>
    ///     优化器引用
    /// </summary>
    public IOptimizer Optimizer { get; set; }

    /// <summary>
    ///     用户自定义数据
    /// </summary>
    public Dictionary<string, object> UserData { get; set; } = new();
}

/// <summary>
///     早停回调 —— 验证损失连续 N 个 epoch 不下降时停止训练
///     防止过拟合，节省训练时间
/// </summary>
public sealed class EarlyStopping : ITrainingCallback
{
    private readonly string _direction;
    private readonly float _minDelta;
    private readonly int _patience;
    private readonly bool _restoreBestWeights;
    private float _bestLoss;
    private Dictionary<string, ArrayND>? _bestWeights;
    private int _wait;

    /// <summary>
    ///     创建早停回调
    /// </summary>
    /// <param name="patience">容忍轮数（连续 N 个 epoch 不改善则停止）</param>
    /// <param name="minDelta">最小改善量</param>
    /// <param name="restoreBestWeights">停止时是否恢复最佳权重</param>
    /// <param name="direction">监控方向："min" 损失越小越好，"max" 指标越大越好</param>
    public EarlyStopping(int patience = 5, float minDelta = 0.0f, bool restoreBestWeights = true,
        string direction = "min")
    {
        _patience = patience;
        _minDelta = minDelta;
        _restoreBestWeights = restoreBestWeights;
        _direction = direction;
        _wait = 0;
        _bestLoss = direction == "min" ? float.MaxValue : float.NegativeInfinity;
        Stopped = false;
        BestEpoch = 0;
    }

    /// <summary>
    ///     是否已触发早停
    /// </summary>
    public bool Stopped { get; private set; }

    /// <summary>
    ///     最佳 epoch
    /// </summary>
    public int BestEpoch { get; private set; }

    /// <summary>
    ///     训练开始时初始化
    /// </summary>
    public void OnTrainBegin(TrainingCallbackState state)
    {
        _wait = 0;
        _bestLoss = _direction == "min" ? float.MaxValue : float.NegativeInfinity;
        Stopped = false;
    }

    /// <summary>
    ///     每个 epoch 结束后检查是否应该早停
    /// </summary>
    public void OnEpochEnd(TrainingCallbackState state)
    {
        var currentLoss = state.EvalLoss;

        var improved = _direction == "min"
            ? currentLoss < _bestLoss - _minDelta
            : currentLoss > _bestLoss + _minDelta;

        if (improved)
        {
            _bestLoss = currentLoss;
            _wait = 0;
            BestEpoch = state.Epoch;

            if (_restoreBestWeights)
            {
                _bestWeights = new Dictionary<string, ArrayND>();
                var idx = 0;
                foreach (var p in state.Model.Parameters())
                {
                    var copy = ArrayND.Zeros(p.Value.Shape);
                    var spanSrc = p.Value.AsSpan();
                    var spanDst = copy.AsWriteSpan();
                    for (var i = 0; i < spanSrc.Length; i++) spanDst[i] = spanSrc[i];
                    _bestWeights[$"p{idx}"] = copy;
                    idx++;
                }
            }
        }
        else
        {
            _wait++;
            if (_wait >= _patience)
            {
                Stopped = true;
                state.StopRequested = true;

                if (_restoreBestWeights && _bestWeights != null)
                {
                    var idx = 0;
                    foreach (var p in state.Model.Parameters())
                    {
                        if (_bestWeights.TryGetValue($"p{idx}", out var saved))
                        {
                            var spanSrc = saved.AsSpan();
                            var spanDst = p.Value.AsWriteSpan();
                            for (var i = 0; i < spanSrc.Length && i < spanDst.Length; i++) spanDst[i] = spanSrc[i];
                        }

                        idx++;
                    }
                }
            }
        }
    }
}

/// <summary>
///     学习率衰减回调 —— 验证损失停滞时降低学习率
///     与 LRScheduler.ReduceLROnPlateau 不同，这是回调版本，在训练循环中自动触发
/// </summary>
public sealed class ReduceLROnPlateauCallback : ITrainingCallback
{
    private readonly float _factor;
    private readonly float _minLR;
    private readonly int _patience;
    private readonly ILRScheduler _scheduler;
    private float _bestLoss;
    private int _wait;

    /// <summary>
    ///     创建学习率衰减回调
    /// </summary>
    /// <param name="scheduler">学习率调度器</param>
    /// <param name="patience">容忍轮数</param>
    /// <param name="factor">衰减因子（新 LR = 旧 LR × factor）</param>
    /// <param name="minLR">最小学习率</param>
    public ReduceLROnPlateauCallback(ILRScheduler scheduler, int patience = 3, float factor = 0.5f, float minLR = 1e-7f)
    {
        _scheduler = scheduler;
        _patience = patience;
        _factor = factor;
        _minLR = minLR;
        _wait = 0;
        _bestLoss = float.MaxValue;
    }

    /// <summary>
    ///     每个 epoch 结束后检查是否应该降低学习率
    /// </summary>
    public void OnEpochEnd(TrainingCallbackState state)
    {
        var currentLoss = state.EvalLoss;

        if (currentLoss < _bestLoss)
        {
            _bestLoss = currentLoss;
            _wait = 0;
        }
        else
        {
            _wait++;
            if (_wait >= _patience)
            {
                var currentLR = _scheduler.LearningRate;
                var newLR = MathF.Max(currentLR * _factor, _minLR);
                _scheduler.SetLearningRate(newLR);
                _wait = 0;
            }
        }
    }
}

/// <summary>
///     训练日志回调 —— 定期输出训练指标
/// </summary>
public sealed class TrainingLogger : ITrainingCallback
{
    private readonly int _logInterval;
    private readonly Action<string> _writeLine;

    /// <summary>
    ///     创建训练日志回调
    /// </summary>
    /// <param name="logInterval">日志间隔（epoch 数）</param>
    /// <param name="writeLine">输出函数（默认 Console.WriteLine）</param>
    public TrainingLogger(int logInterval = 1, Action<string>? writeLine = null)
    {
        _logInterval = logInterval;
        _writeLine = writeLine ?? System.Console.WriteLine;
    }

    /// <summary>
    ///     日志条目列表
    /// </summary>
    public List<string> Entries { get; } = [];

    /// <summary>
    ///     每个 epoch 结束后记录日志
    /// </summary>
    public void OnEpochEnd(TrainingCallbackState state)
    {
        if (state.Epoch % _logInterval != 0 && state.Epoch > 0) return;

        var msg = $"[Epoch {state.Epoch}] loss={state.TrainLoss:F4}" +
                  (state.EvalLoss > 0 ? $" eval_loss={state.EvalLoss:F4} ppl={state.EvalPerplexity:F2}" : "") +
                  $" lr={state.LearningRate:E2} step={state.GlobalStep}";
        Entries.Add(msg);
        _writeLine(msg);
    }
}

/// <summary>
///     模型检查点回调 —— 定期保存模型检查点
/// </summary>
public sealed class ModelCheckpoint : ITrainingCallback
{
    private readonly bool _saveBestOnly;
    private readonly int _saveInterval;
    private readonly string _savePrefix;
    private float _bestLoss;

    /// <summary>
    ///     创建模型检查点回调
    /// </summary>
    /// <param name="saveInterval">保存间隔（epoch 数）</param>
    /// <param name="savePrefix">保存前缀</param>
    /// <param name="saveBestOnly">是否只保存最佳模型</param>
    public ModelCheckpoint(int saveInterval = 5, string savePrefix = "model", bool saveBestOnly = false)
    {
        _saveInterval = saveInterval;
        _savePrefix = savePrefix;
        _saveBestOnly = saveBestOnly;
        _bestLoss = float.MaxValue;
    }

    /// <summary>
    ///     保存的检查点列表
    /// </summary>
    public List<(int epoch, float loss, Dictionary<string, ArrayND> weights)> Checkpoints { get; } = [];

    /// <summary>
    ///     每个 epoch 结束后检查是否应该保存检查点
    /// </summary>
    public void OnEpochEnd(TrainingCallbackState state)
    {
        var shouldSave = false;

        if (_saveBestOnly)
        {
            if (state.EvalLoss < _bestLoss)
            {
                _bestLoss = state.EvalLoss;
                shouldSave = true;
            }
        }
        else
        {
            shouldSave = (state.Epoch + 1) % _saveInterval == 0;
        }

        if (!shouldSave) return;

        var weights = new Dictionary<string, ArrayND>();
        var idx = 0;
        foreach (var p in state.Model.Parameters())
        {
            var copy = ArrayND.Zeros(p.Value.Shape);
            var spanSrc = p.Value.AsSpan();
            var spanDst = copy.AsWriteSpan();
            for (var i = 0; i < spanSrc.Length; i++) spanDst[i] = spanSrc[i];
            weights[$"p{idx}"] = copy;
            idx++;
        }

        Checkpoints.Add((state.Epoch, state.EvalLoss, weights));
    }
}

/// <summary>
///     带回调的训练器 —— 在 LanguageModelTrainer 基础上支持回调机制
/// </summary>
public sealed class CallbackTrainer
{
    private readonly int _accumulationSteps;
    private readonly List<ITrainingCallback> _callbacks;
    private readonly Func<ITrainableModel, ArrayND, AutogradContext, ArrayND>? _forwardFn;
    private readonly Func<ArrayND, ArrayND, AutogradContext, (ArrayND loss, ArrayND? aux)> _lossFn;
    private readonly ILRScheduler? _lrScheduler;
    private readonly float _maxGradNorm;
    private readonly ITrainableModel _model;
    private readonly IOptimizer _optimizer;

    /// <summary>
    ///     创建带回调的训练器
    /// </summary>
    /// <param name="model">模型</param>
    /// <param name="optimizer">优化器</param>
    /// <param name="lrScheduler">学习率调度器</param>
    /// <param name="maxGradNorm">梯度裁剪阈值</param>
    /// <param name="accumulationSteps">梯度累积步数</param>
    /// <param name="forwardFn">自定义前向函数</param>
    /// <param name="lossFn">损失函数</param>
    /// <param name="callbacks">回调列表</param>
    public CallbackTrainer(
        ITrainableModel model,
        IOptimizer optimizer,
        ILRScheduler? lrScheduler = null,
        float maxGradNorm = 1.0f,
        int accumulationSteps = 1,
        Func<ITrainableModel, ArrayND, AutogradContext, ArrayND>? forwardFn = null,
        Func<ArrayND, ArrayND, AutogradContext, (ArrayND loss, ArrayND? aux)>? lossFn = null,
        params ITrainingCallback[] callbacks)
    {
        _model = model;
        _optimizer = optimizer;
        _lrScheduler = lrScheduler;
        _maxGradNorm = maxGradNorm;
        _accumulationSteps = System.Math.Max(1, accumulationSteps);
        _forwardFn = forwardFn;
        _lossFn = lossFn ?? ((logits, targets, ctx) =>
        {
            var (l, _) = Losses.SoftmaxCrossEntropy(logits, targets, ctx);
            return (l, null);
        });
        _callbacks = [.. callbacks];
    }

    /// <summary>
    ///     全局步数
    /// </summary>
    public int GlobalStep { get; private set; }

    /// <summary>
    ///     当前 epoch
    /// </summary>
    public int Epoch { get; private set; }

    /// <summary>
    ///     添加回调
    /// </summary>
    /// <param name="callback">回调</param>
    public void AddCallback(ITrainingCallback callback)
    {
        _callbacks.Add(callback);
    }

    /// <summary>
    ///     完整训练循环（带回调）
    /// </summary>
    /// <param name="trainInputs">训练输入</param>
    /// <param name="trainLabels">训练标签</param>
    /// <param name="evalInputs">评估输入</param>
    /// <param name="evalLabels">评估标签</param>
    /// <param name="epochs">训练轮数</param>
    /// <param name="batchSize">批次大小</param>
    /// <returns>训练历史</returns>
    public LanguageModelHistory Fit(
        ArrayND trainInputs, ArrayND trainLabels,
        ArrayND evalInputs, ArrayND evalLabels,
        int epochs, int batchSize)
    {
        var state = new TrainingCallbackState(0, 0, _model, _optimizer);
        FireCallbacks(cb => cb.OnTrainBegin(state));

        var history = new LanguageModelHistory(epochs);

        for (var epoch = 0; epoch < epochs; epoch++)
        {
            state.Epoch = epoch;
            state.StopRequested = false;
            FireCallbacks(cb => cb.OnEpochBegin(state));

            var trainLoss = TrainEpoch(trainInputs, trainLabels, batchSize);
            var evalResult = Evaluate(evalInputs, evalLabels, batchSize);

            state.TrainLoss = trainLoss;
            state.EvalLoss = evalResult.Loss;
            state.EvalPerplexity = evalResult.Perplexity;
            state.LearningRate = _lrScheduler?.LearningRate ?? 0;

            history.Record(epoch, trainLoss, evalResult.Loss, evalResult.Perplexity);

            FireCallbacks(cb => cb.OnEpochEnd(state));

            if (state.StopRequested) break;
        }

        FireCallbacks(cb => cb.OnTrainEnd(state));

        return history;
    }

    private float TrainEpoch(ArrayND dataInputs, ArrayND dataLabels, int batchSize)
    {
        var totalSamples = dataInputs.Shape[0];
        var totalLoss = 0.0f;
        var numBatches = 0;

        var indices = ShuffleIndices(totalSamples);

        for (var start = 0; start < totalSamples; start += batchSize)
        {
            var end = System.Math.Min(start + batchSize, totalSamples);
            var actualBatchSize = end - start;

            var batchInputs = GatherRows(dataInputs, indices, start, actualBatchSize);
            var batchLabels = GatherRows(dataLabels, indices, start, actualBatchSize);

            var loss = TrainStep(batchInputs, batchLabels);
            totalLoss += loss;
            numBatches++;
        }

        Epoch++;
        return numBatches > 0 ? totalLoss / numBatches : 0.0f;
    }

    private float TrainStep(ArrayND inputs, ArrayND targets)
    {
        var ctx = new AutogradContext();
        ctx.StartRecording();

        ArrayND logits;
        if (_forwardFn != null)
            logits = _forwardFn(_model, inputs, ctx);
        else if (_model is GPTModel gptModel)
            logits = gptModel.ForwardForTraining(inputs, ctx);
        else if (_model is WeightTiedGPTModel wtModel)
            logits = wtModel.ForwardForTraining(inputs, ctx);
        else
            logits = _model.forward(inputs, ctx);

        var (lossTensor, _) = _lossFn(logits, targets, ctx);
        ctx.Backward(lossTensor);

        var loss = lossTensor.AsSpan()[0];

        if (_maxGradNorm > 0)
        {
            var allParams = _model.Parameters().ToList();
            GradientClipping.ClipGradNorm(allParams, _maxGradNorm);
        }

        var paramList = _model.Parameters().ToList();
        _optimizer.Step(paramList);
        _optimizer.ZeroGrad(paramList);

        _lrScheduler?.Step();
        GlobalStep++;

        return loss;
    }

    private EvalResult Evaluate(ArrayND dataInputs, ArrayND dataLabels, int batchSize)
    {
        var totalSamples = dataInputs.Shape[0];
        var totalLoss = 0.0f;
        var numBatches = 0;

        for (var start = 0; start < totalSamples; start += batchSize)
        {
            var end = System.Math.Min(start + batchSize, totalSamples);
            var actualBatchSize = end - start;

            var batchInputs = dataInputs.SliceRows(start, actualBatchSize);
            var batchLabels = dataLabels.SliceRows(start, actualBatchSize);

            ArrayND logits;
            if (_model is GPTModel gptModel)
                logits = gptModel.ForwardForTraining(batchInputs, new AutogradContext());
            else if (_model is WeightTiedGPTModel wtModel)
                logits = wtModel.ForwardForTraining(batchInputs, new AutogradContext());
            else
                logits = _model.forward(batchInputs);

            var (lossVal, _) = Losses.SoftmaxCrossEntropyForward(logits, batchLabels);
            totalLoss += lossVal;
            numBatches++;
        }

        var avgLoss = numBatches > 0 ? totalLoss / numBatches : 0.0f;
        return new EvalResult(avgLoss, MathF.Exp(avgLoss));
    }

    private void FireCallbacks(Action<ITrainingCallback> action)
    {
        foreach (var cb in _callbacks) action(cb);
    }

    private static int[] ShuffleIndices(int count)
    {
        var indices = new int[count];
        for (var i = 0; i < count; i++) indices[i] = i;
        var rng = Random.Shared;
        for (var i = count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        return indices;
    }

    private static ArrayND GatherRows(ArrayND source, int[] indices, int start, int count)
    {
        var rowSize = 1;
        for (var d = 1; d < source.Shape.Length; d++) rowSize *= source.Shape[d];

        var fullShape = new int[source.Shape.Length];
        fullShape[0] = count;
        for (var d = 1; d < source.Shape.Length; d++) fullShape[d] = source.Shape[d];

        var result = ArrayND.Zeros(fullShape);
        var spanSrc = source.AsSpan();
        var spanDst = result.AsWriteSpan();

        for (var i = 0; i < count; i++)
        {
            var srcIdx = start + i < indices.Length ? indices[start + i] : start + i;
            var srcOff = srcIdx * rowSize;
            var dstOff = i * rowSize;
            for (var j = 0; j < rowSize; j++) spanDst[dstOff + j] = spanSrc[srcOff + j];
        }

        return result;
    }
}
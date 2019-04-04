using Std.DL.Training;

namespace Std.DL.Flux;

/// <summary>
///     语言模型训练器 —— 专为 GPT / LLM 训练设计的训练循环编排器
///     支持：ForwardForTraining、LR 调度、梯度裁剪、梯度累积、检查点、指标追踪
/// </summary>
public sealed class LanguageModelTrainer
{
    private readonly int _accumulationSteps;
    private readonly int _checkpointInterval;
    private readonly Func<ITrainableModel, ArrayND, AutogradContext, ArrayND>? _forwardFn;
    private readonly List<TrainingLogEntry> _log;
    private readonly int _logInterval;
    private readonly Func<ArrayND, ArrayND, AutogradContext, (ArrayND loss, ArrayND? aux)> _lossFn;
    private readonly ILRScheduler? _lrScheduler;
    private readonly float _maxGradNorm;
    private readonly ITrainableModel _model;
    private readonly IOptimizer _optimizer;

    /// <summary>
    ///     创建语言模型训练器
    /// </summary>
    /// <param name="model">待训练模型</param>
    /// <param name="optimizer">优化器</param>
    /// <param name="lrScheduler">学习率调度器（可选）</param>
    /// <param name="maxGradNorm">梯度裁剪阈值（0 表示不裁剪）</param>
    /// <param name="accumulationSteps">梯度累积步数（1 表示不累积）</param>
    /// <param name="logInterval">日志输出间隔（步数）</param>
    /// <param name="checkpointInterval">检查点保存间隔（epoch 数）</param>
    /// <param name="forwardFn">自定义前向函数（默认使用 ForwardForTraining 模式）</param>
    /// <param name="lossFn">损失函数（默认使用 SoftmaxCrossEntropy）</param>
    public LanguageModelTrainer(
        ITrainableModel model,
        IOptimizer optimizer,
        ILRScheduler? lrScheduler = null,
        float maxGradNorm = 1.0f,
        int accumulationSteps = 1,
        int logInterval = 10,
        int checkpointInterval = 5,
        Func<ITrainableModel, ArrayND, AutogradContext, ArrayND>? forwardFn = null,
        Func<ArrayND, ArrayND, AutogradContext, (ArrayND loss, ArrayND? aux)>? lossFn = null)
    {
        _model = model;
        _optimizer = optimizer;
        _lrScheduler = lrScheduler;
        _maxGradNorm = maxGradNorm;
        _accumulationSteps = System.Math.Max(1, accumulationSteps);
        _logInterval = logInterval;
        _checkpointInterval = checkpointInterval;
        _forwardFn = forwardFn;
        _lossFn = lossFn ?? ((logits, targets, ctx) =>
        {
            var (l, _) = Losses.SoftmaxCrossEntropy(logits, targets, ctx);
            return (l, null);
        });

        GlobalStep = 0;
        Epoch = 0;
        BestLoss = float.MaxValue;
        _log = [];
    }

    /// <summary>
    ///     全局训练步数
    /// </summary>
    public int GlobalStep { get; private set; }

    /// <summary>
    ///     当前 epoch
    /// </summary>
    public int Epoch { get; private set; }

    /// <summary>
    ///     最佳损失值
    /// </summary>
    public float BestLoss { get; private set; }

    /// <summary>
    ///     训练日志
    /// </summary>
    public IReadOnlyList<TrainingLogEntry> Log => _log;

    /// <summary>
    ///     执行一个训练步骤
    /// </summary>
    /// <param name="inputs">输入 [batch, seqLen] 或 [batch, features]</param>
    /// <param name="targets">目标 [batch, 1]</param>
    /// <returns>本步损失值</returns>
    public float TrainStep(ArrayND inputs, ArrayND targets)
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

        if (_accumulationSteps > 1)
        {
            GlobalStep++;
            if (GlobalStep % _accumulationSteps == 0)
            {
                var paramList = _model.Parameters().ToList();
                foreach (var p in paramList)
                {
                    if (p.Grad == null) continue;

                    var span = p.Grad.AsWriteSpan();
                    var scale = 1.0f / _accumulationSteps;
                    for (var i = 0; i < span.Length; i++) span[i] *= scale;
                }

                _optimizer.Step(paramList);
                _optimizer.ZeroGrad(paramList);
            }
        }
        else
        {
            var paramList = _model.Parameters().ToList();
            _optimizer.Step(paramList);
            _optimizer.ZeroGrad(paramList);
        }

        _lrScheduler?.Step();

        if (GlobalStep % _logInterval == 0 || GlobalStep <= 1)
        {
            var lr = _lrScheduler?.LearningRate ?? 0;
            _log.Add(new TrainingLogEntry(GlobalStep, Epoch, loss, lr));
        }

        return loss;
    }

    /// <summary>
    ///     训练一个 epoch
    /// </summary>
    /// <param name="dataInputs">全部训练输入 [N, seqLen]</param>
    /// <param name="dataLabels">全部训练标签 [N, 1]</param>
    /// <param name="batchSize">批次大小</param>
    /// <returns>epoch 平均损失</returns>
    public float TrainEpoch(ArrayND dataInputs, ArrayND dataLabels, int batchSize)
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

        var avgLoss = numBatches > 0 ? totalLoss / numBatches : 0.0f;

        if (avgLoss < BestLoss) BestLoss = avgLoss;

        return avgLoss;
    }

    /// <summary>
    ///     评估损失和指标
    /// </summary>
    /// <param name="dataInputs">输入 [N, seqLen]</param>
    /// <param name="dataLabels">标签 [N, 1]</param>
    /// <param name="batchSize">批次大小</param>
    /// <returns>评估结果</returns>
    public EvalResult Evaluate(ArrayND dataInputs, ArrayND dataLabels, int batchSize)
    {
        var totalSamples = dataInputs.Shape[0];
        var totalLoss = 0.0f;
        var numBatches = 0;
        var allLogits = new List<ArrayND>();
        var allTargets = new List<ArrayND>();

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

            allLogits.Add(logits);
            allTargets.Add(batchLabels);
        }

        var avgLoss = numBatches > 0 ? totalLoss / numBatches : 0.0f;
        var ppl = MathF.Exp(avgLoss);

        return new EvalResult(avgLoss, ppl);
    }

    /// <summary>
    ///     完整训练循环
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
        var history = new LanguageModelHistory(epochs);

        for (var epoch = 0; epoch < epochs; epoch++)
        {
            var trainLoss = TrainEpoch(trainInputs, trainLabels, batchSize);
            var evalResult = Evaluate(evalInputs, evalLabels, batchSize);

            history.Record(epoch, trainLoss, evalResult.Loss, evalResult.Perplexity);

            if ((epoch + 1) % _checkpointInterval == 0)
            {
                var checkpoint = TrainingState.SaveCheckpoint(_model.Parameters(), epoch, trainLoss);
                history.AddCheckpoint(epoch, checkpoint);
            }
        }

        return history;
    }

    /// <summary>
    ///     从检查点恢复训练状态
    /// </summary>
    /// <param name="checkpoint">检查点字典</param>
    public void RestoreFromCheckpoint(Dictionary<string, object> checkpoint)
    {
        var (epoch, loss) = TrainingState.LoadCheckpoint(_model.Parameters(), checkpoint);
        Epoch = epoch;
        BestLoss = System.Math.Min(BestLoss, loss);
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
        var srcRows = source.Shape[0];
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
            if (srcIdx >= srcRows) srcIdx = srcRows - 1;

            var srcOff = srcIdx * rowSize;
            var dstOff = i * rowSize;
            for (var j = 0; j < rowSize; j++) spanDst[dstOff + j] = spanSrc[srcOff + j];
        }

        return result;
    }
}

/// <summary>
///     训练日志条目
/// </summary>
public sealed class TrainingLogEntry
{
    /// <summary>
    ///     创建训练日志条目
    /// </summary>
    /// <param name="step">全局步数</param>
    /// <param name="epoch">Epoch 编号</param>
    /// <param name="loss">损失值</param>
    /// <param name="learningRate">学习率</param>
    public TrainingLogEntry(int step, int epoch, float loss, float learningRate)
    {
        Step = step;
        Epoch = epoch;
        Loss = loss;
        LearningRate = learningRate;
    }

    /// <summary>
    ///     全局步数
    /// </summary>
    public int Step { get; }

    /// <summary>
    ///     Epoch 编号
    /// </summary>
    public int Epoch { get; }

    /// <summary>
    ///     损失值
    /// </summary>
    public float Loss { get; }

    /// <summary>
    ///     学习率
    /// </summary>
    public float LearningRate { get; }
}

/// <summary>
///     评估结果
/// </summary>
public sealed class EvalResult
{
    /// <summary>
    ///     创建评估结果
    /// </summary>
    /// <param name="loss">平均损失</param>
    /// <param name="perplexity">困惑度</param>
    public EvalResult(float loss, float perplexity)
    {
        Loss = loss;
        Perplexity = perplexity;
    }

    /// <summary>
    ///     平均损失
    /// </summary>
    public float Loss { get; }

    /// <summary>
    ///     困惑度
    /// </summary>
    public float Perplexity { get; }
}

/// <summary>
///     语言模型训练历史
/// </summary>
public sealed class LanguageModelHistory
{
    private readonly Dictionary<int, Dictionary<string, object>> _checkpoints;
    private readonly List<float> _evalLosses;
    private readonly List<float> _perplexities;
    private readonly List<float> _trainLosses;

    /// <summary>
    ///     创建语言模型训练历史
    /// </summary>
    /// <param name="expectedEpochs">预期 epoch 数</param>
    public LanguageModelHistory(int expectedEpochs = 0)
    {
        _trainLosses = new(expectedEpochs);
        _evalLosses = new(expectedEpochs);
        _perplexities = new(expectedEpochs);
        _checkpoints = new Dictionary<int, Dictionary<string, object>>();
    }

    /// <summary>
    ///     训练损失列表
    /// </summary>
    public IReadOnlyList<float> TrainLosses => _trainLosses;

    /// <summary>
    ///     评估损失列表
    /// </summary>
    public IReadOnlyList<float> EvalLosses => _evalLosses;

    /// <summary>
    ///     困惑度列表
    /// </summary>
    public IReadOnlyList<float> Perplexities => _perplexities;

    /// <summary>
    ///     检查点集合
    /// </summary>
    public IReadOnlyDictionary<int, Dictionary<string, object>> Checkpoints => _checkpoints;

    /// <summary>
    ///     记录一个 epoch 的结果
    /// </summary>
    /// <param name="epoch">epoch 编号</param>
    /// <param name="trainLoss">训练损失</param>
    /// <param name="evalLoss">评估损失</param>
    /// <param name="perplexity">困惑度</param>
    public void Record(int epoch, float trainLoss, float evalLoss, float perplexity)
    {
        _trainLosses.Add(trainLoss);
        _evalLosses.Add(evalLoss);
        _perplexities.Add(perplexity);
    }

    /// <summary>
    ///     添加检查点
    /// </summary>
    /// <param name="epoch">epoch 编号</param>
    /// <param name="checkpoint">检查点数据</param>
    public void AddCheckpoint(int epoch, Dictionary<string, object> checkpoint)
    {
        _checkpoints[epoch] = checkpoint;
    }
}
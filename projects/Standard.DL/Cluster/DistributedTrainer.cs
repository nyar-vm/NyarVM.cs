using Std.DL.Flux;
using Std.DL.Training;

namespace Std.DL.Cluster;

/// <summary>
///     分布式数据并行训练器 —— 将批次数据分片到多个工作节点并行计算，
///     通过梯度累加模拟 AllReduce 同步，然后统一更新参数
/// </summary>
public sealed class DistributedTrainer
{
    private readonly ITrainableModel _model;
    private readonly IOptimizer _optimizer;
    private readonly List<DistributedWorkerMetrics> _workerMetrics;

    /// <summary>
    ///     创建分布式训练器
    /// </summary>
    /// <param name="model">待训练模型</param>
    /// <param name="optimizer">优化器</param>
    /// <param name="numWorkers">工作节点数</param>
    public DistributedTrainer(ITrainableModel model, IOptimizer optimizer, int numWorkers = 2)
    {
        _model = model;
        _optimizer = optimizer;
        WorkerCount = System.Math.Max(1, numWorkers);
        _workerMetrics = [];
    }

    /// <summary>
    ///     分布式训练历史记录
    /// </summary>
    public DistributedTrainingHistory? LastHistory { get; private set; }

    /// <summary>
    ///     获取当前 worker 的指标快照
    /// </summary>
    public IReadOnlyList<DistributedWorkerMetrics> WorkerMetrics => _workerMetrics;

    /// <summary>
    ///     获取 worker 数量
    /// </summary>
    public int WorkerCount { get; }

    /// <summary>
    ///     分布式训练步骤：
    ///     数据分片 → 各节点独立前向+反向 → 梯度累加 → AllReduce 平均 → 参数更新
    /// </summary>
    /// <param name="inputs">输入张量 [batch, features]</param>
    /// <param name="labels">标签 [batch, 1]（索引编码）</param>
    /// <returns>平均训练损失</returns>
    public float DistributedTrainStep(ArrayND inputs, ArrayND labels)
    {
        var totalSamples = inputs.Shape[0];
        var samplesPerWorker = totalSamples / WorkerCount;

        if (samplesPerWorker < 1) return FallbackTrainStep(inputs, labels);

        var totalLoss = 0.0f;
        _workerMetrics.Clear();

        for (var w = 0; w < WorkerCount; w++)
        {
            var start = w * samplesPerWorker;
            var end = w == WorkerCount - 1 ? totalSamples : start + samplesPerWorker;
            var actualBatch = end - start;

            var workerInputs = inputs.SliceRows(start, actualBatch);
            var workerLabels = labels.SliceRows(start, actualBatch);

            var ctx = new AutogradContext();
            ctx.StartRecording();

            var logits = _model.forward(workerInputs, ctx);
            var (lossTensor, _) = Losses.SoftmaxCrossEntropy(logits, workerLabels, ctx);

            ctx.Backward(lossTensor);

            var lossVal = lossTensor.AsSpan()[0];
            totalLoss += lossVal;

            _workerMetrics.Add(new DistributedWorkerMetrics
            {
                WorkerId = w,
                BatchSize = actualBatch,
                Loss = lossVal
            });
        }

        var parameters = _model.Parameters().ToList();
        foreach (var p in parameters)
        {
            var grad = p.Grad;
            if (grad is not null)
            {
                var spanGrad = grad.AsWriteSpan();
                for (var i = 0; i < spanGrad.Length; i++) spanGrad[i] /= WorkerCount;
            }
        }

        _optimizer.Step(parameters);
        _optimizer.ZeroGrad(parameters);

        return totalLoss / WorkerCount;
    }

    /// <summary>
    ///     分布式训练一个 epoch
    /// </summary>
    /// <param name="dataInputs">全部训练输入 [N, features]</param>
    /// <param name="dataLabels">全部训练标签 [N, 1]</param>
    /// <param name="batchSize">每个节点每批次的样本数</param>
    /// <returns>epoch 平均损失</returns>
    public float DistributedTrainEpoch(ArrayND dataInputs, ArrayND dataLabels, int batchSize)
    {
        var totalSamples = dataInputs.Shape[0];
        var effectiveBatchSize = batchSize * WorkerCount;
        var totalLoss = 0.0f;
        var numBatches = 0;

        for (var start = 0; start < totalSamples; start += effectiveBatchSize)
        {
            var end = System.Math.Min(start + effectiveBatchSize, totalSamples);
            var actualBatch = end - start;
            var batchInputs = dataInputs.SliceRows(start, actualBatch);
            var batchLabels = dataLabels.SliceRows(start, actualBatch);

            var loss = DistributedTrainStep(batchInputs, batchLabels);
            totalLoss += loss;
            numBatches++;
        }

        return numBatches > 0 ? totalLoss / numBatches : 0.0f;
    }

    /// <summary>
    ///     分布式评估
    /// </summary>
    /// <param name="dataInputs">全部评估输入 [N, features]</param>
    /// <param name="dataLabels">全部评估标签 [N, 1]</param>
    /// <param name="batchSize">批次大小</param>
    /// <returns>(平均损失, 准确率)</returns>
    public (float Loss, float Accuracy) Evaluate(ArrayND dataInputs, ArrayND dataLabels, int batchSize)
    {
        var totalSamples = dataInputs.Shape[0];
        var totalLoss = 0.0f;
        var numBatches = 0;
        var totalCorrect = 0;

        for (var start = 0; start < totalSamples; start += batchSize)
        {
            var end = System.Math.Min(start + batchSize, totalSamples);
            var actualBatchSize = end - start;

            var batchInputs = dataInputs.SliceRows(start, actualBatchSize);
            var batchLabels = dataLabels.SliceRows(start, actualBatchSize);

            var logits = _model.forward(batchInputs);
            var classCount = logits.Shape[1];

            var (lossVal, probs) = Losses.SoftmaxCrossEntropyForward(logits, batchLabels);
            totalLoss += lossVal;

            var spanProbs = probs.AsSpan();
            var spanLabels = batchLabels.AsSpan();
            var correct = 0;
            for (var i = 0; i < actualBatchSize; i++)
            {
                var maxIdx = 0;
                var maxVal = float.MinValue;
                for (var j = 0; j < classCount; j++)
                {
                    var val = spanProbs[i * classCount + j];
                    if (j == 0 || val > maxVal)
                    {
                        maxVal = val;
                        maxIdx = j;
                    }
                }

                if (maxIdx == (int)spanLabels[i]) correct++;
            }

            totalCorrect += correct;
            numBatches++;
        }

        var avgLoss = numBatches > 0 ? totalLoss / numBatches : 0.0f;
        var accuracy = totalSamples > 0 ? (float)totalCorrect / totalSamples : 0.0f;

        return (avgLoss, accuracy);
    }

    /// <summary>
    ///     完整的分布式训练循环
    /// </summary>
    /// <param name="trainInputs">训练输入 [N, features]</param>
    /// <param name="trainLabels">训练标签 [N, 1]</param>
    /// <param name="evalInputs">评估输入 [M, features]</param>
    /// <param name="evalLabels">评估标签 [M, 1]</param>
    /// <param name="epochs">训练轮数</param>
    /// <param name="batchSize">每个节点的批次大小</param>
    /// <returns>分布式训练历史记录</returns>
    public DistributedTrainingHistory Fit(
        ArrayND trainInputs, ArrayND trainLabels,
        ArrayND evalInputs, ArrayND evalLabels,
        int epochs, int batchSize)
    {
        var history = new DistributedTrainingHistory(epochs);

        for (var epoch = 0; epoch < epochs; epoch++)
        {
            var trainLoss = DistributedTrainEpoch(trainInputs, trainLabels, batchSize);
            var (evalLoss, evalAcc) = Evaluate(evalInputs, evalLabels, batchSize);

            var throughSamples = (epoch + 1) * trainInputs.Shape[0];
            history.Record(epoch, trainLoss, evalLoss, evalAcc, throughSamples);
        }

        LastHistory = history;
        return history;
    }

    /// <summary>
    ///     退化为单节点训练步骤（样本数不足时使用）
    /// </summary>
    private float FallbackTrainStep(ArrayND inputs, ArrayND labels)
    {
        var ctx = new AutogradContext();
        ctx.StartRecording();

        var logits = _model.forward(inputs, ctx);
        var (lossTensor, _) = Losses.SoftmaxCrossEntropy(logits, labels, ctx);

        ctx.Backward(lossTensor);

        var parameters = _model.Parameters().ToList();
        _optimizer.Step(parameters);
        _optimizer.ZeroGrad(parameters);

        return lossTensor.AsSpan()[0];
    }
}

/// <summary>
///     分布式 Worker 度量信息
/// </summary>
public sealed class DistributedWorkerMetrics
{
    /// <summary>Worker ID</summary>
    public int WorkerId { get; init; }

    /// <summary>处理的样本数</summary>
    public int BatchSize { get; init; }

    /// <summary>当前损失值</summary>
    public float Loss { get; init; }
}

/// <summary>
///     分布式训练历史记录
/// </summary>
public sealed class DistributedTrainingHistory
{
    private readonly List<float> _evalAccuracies;
    private readonly List<float> _evalLosses;
    private readonly List<int> _throughSamples;
    private readonly List<float> _trainLosses;

    /// <summary>
    ///     创建分布式训练历史记录
    /// </summary>
    public DistributedTrainingHistory(int expectedEpochs = 0)
    {
        _trainLosses = new(expectedEpochs);
        _evalLosses = new(expectedEpochs);
        _evalAccuracies = new(expectedEpochs);
        _throughSamples = new(expectedEpochs);
    }

    /// <summary>训练损失</summary>
    public IReadOnlyList<float> TrainLosses => _trainLosses;

    /// <summary>评估损失</summary>
    public IReadOnlyList<float> EvalLosses => _evalLosses;

    /// <summary>评估准确率</summary>
    public IReadOnlyList<float> EvalAccuracies => _evalAccuracies;

    /// <summary>累计处理样本数</summary>
    public IReadOnlyList<int> ThroughSamples => _throughSamples;

    /// <summary>
    ///     记录一个 epoch
    /// </summary>
    public void Record(int epoch, float trainLoss, float evalLoss, float evalAcc, int through)
    {
        _trainLosses.Add(trainLoss);
        _evalLosses.Add(evalLoss);
        _evalAccuracies.Add(evalAcc);
        _throughSamples.Add(through);
    }
}
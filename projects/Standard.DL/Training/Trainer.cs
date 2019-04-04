using Std.DL.Flux;

namespace Std.DL.Training;

/// <summary>
///     训练器 —— 封装模型训练循环（前向 → 损失 → 反向 → 更新）
/// </summary>
public sealed class Trainer
{
    private readonly ITrainableModel _model;
    private readonly IOptimizer _optimizer;

    /// <summary>
    ///     创建训练器
    /// </summary>
    /// <param name="model">待训练模型</param>
    /// <param name="optimizer">优化器</param>
    public Trainer(ITrainableModel model, IOptimizer optimizer)
    {
        _model = model;
        _optimizer = optimizer;
    }

    /// <summary>
    ///     执行一个训练步骤：前向 → 损失 → 反向 → 参数更新
    /// </summary>
    /// <param name="inputs">输入张量 [batch, features]</param>
    /// <param name="labels">标签 [batch, 1]（索引编码）</param>
    /// <returns>训练损失值</returns>
    public float train_step(ArrayND inputs, ArrayND labels)
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

    /// <summary>
    ///     执行一个评估步骤：前向 → 损失 → 准确率
    /// </summary>
    /// <param name="inputs">输入张量 [batch, features]</param>
    /// <param name="labels">标签 [batch, 1]（索引编码）</param>
    /// <returns>(损失值, 准确率)</returns>
    public (float Loss, float Accuracy) eval_step(ArrayND inputs, ArrayND labels)
    {
        var logits = _model.forward(inputs);
        var batch = logits.Shape[0];
        var classes = logits.Shape[1];

        var (lossVal, probs) = Losses.SoftmaxCrossEntropyForward(logits, labels);

        var spanProbs = probs.AsSpan();
        var spanLabels = labels.AsSpan();
        var correct = 0;

        for (var i = 0; i < batch; i++)
        {
            var maxIdx = 0;
            var maxVal = float.MinValue;
            for (var j = 0; j < classes; j++)
            {
                var val = spanProbs[i * classes + j];
                if (val > maxVal)
                {
                    maxVal = val;
                    maxIdx = j;
                }
            }

            if (maxIdx == (int)spanLabels[i]) correct++;
        }

        return (lossVal, (float)correct / batch);
    }

    /// <summary>
    ///     训练一个 epoch
    /// </summary>
    /// <param name="dataInputs">全部训练输入 [N, features]</param>
    /// <param name="dataLabels">全部训练标签 [N, 1]</param>
    /// <param name="batchSize">批次大小</param>
    /// <returns>epoch 平均损失</returns>
    public float train_epoch(ArrayND dataInputs, ArrayND dataLabels, int batchSize)
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

            var loss = train_step(batchInputs, batchLabels);
            totalLoss += loss;
            numBatches++;
        }

        return numBatches > 0 ? totalLoss / numBatches : 0.0f;
    }

    /// <summary>
    ///     评估整个数据集
    /// </summary>
    /// <param name="dataInputs">全部输入 [N, features]</param>
    /// <param name="dataLabels">全部标签 [N, 1]</param>
    /// <param name="batchSize">批次大小</param>
    /// <returns>(平均损失, 准确率)</returns>
    public (float Loss, float Accuracy) evaluate(ArrayND dataInputs, ArrayND dataLabels, int batchSize)
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

            var (loss, acc) = eval_step(batchInputs, batchLabels);
            totalLoss += loss;
            totalCorrect += (int)(acc * actualBatchSize);
            numBatches++;
        }

        var avgLoss = numBatches > 0 ? totalLoss / numBatches : 0.0f;
        var accuracy = totalSamples > 0 ? (float)totalCorrect / totalSamples : 0.0f;

        return (avgLoss, accuracy);
    }

    /// <summary>
    ///     完整的训练循环
    /// </summary>
    /// <param name="trainInputs">训练输入 [N, features]</param>
    /// <param name="trainLabels">训练标签 [N, 1]</param>
    /// <param name="evalInputs">评估输入 [M, features]</param>
    /// <param name="evalLabels">评估标签 [M, 1]</param>
    /// <param name="epochs">训练轮数</param>
    /// <param name="batchSize">批次大小</param>
    /// <returns>训练历史记录</returns>
    public TrainingHistory fit(
        ArrayND trainInputs, ArrayND trainLabels,
        ArrayND evalInputs, ArrayND evalLabels,
        int epochs, int batchSize)
    {
        var history = new TrainingHistory(epochs);

        for (var epoch = 0; epoch < epochs; epoch++)
        {
            var trainLoss = train_epoch(trainInputs, trainLabels, batchSize);
            var (evalLoss, evalAcc) = evaluate(evalInputs, evalLabels, batchSize);

            history.record(epoch, trainLoss, evalLoss, evalAcc);
        }

        return history;
    }
}

/// <summary>
///     训练历史记录 —— 跟踪每个 epoch 的损失和准确率
/// </summary>
public sealed class TrainingHistory
{
    private readonly List<float> _eval_accuracies;
    private readonly List<float> _eval_losses;
    private readonly List<float> _train_losses;

    /// <summary>
    ///     创建训练历史记录
    /// </summary>
    /// <param name="expectedEpochs">预期 epoch 数（用于预分配）</param>
    public TrainingHistory(int expectedEpochs = 0)
    {
        _train_losses = new(expectedEpochs);
        _eval_losses = new(expectedEpochs);
        _eval_accuracies = new(expectedEpochs);
    }

    /// <summary>
    ///     训练损失列表
    /// </summary>
    public IReadOnlyList<float> train_losses => _train_losses;

    /// <summary>
    ///     评估损失列表
    /// </summary>
    public IReadOnlyList<float> eval_losses => _eval_losses;

    /// <summary>
    ///     评估准确率列表
    /// </summary>
    public IReadOnlyList<float> eval_accuracies => _eval_accuracies;

    /// <summary>
    ///     记录一个 epoch 的结果
    /// </summary>
    /// <param name="epoch">epoch 编号</param>
    /// <param name="trainLoss">训练损失</param>
    /// <param name="evalLoss">评估损失</param>
    /// <param name="evalAcc">评估准确率</param>
    public void record(int epoch, float trainLoss, float evalLoss, float evalAcc)
    {
        _train_losses.Add(trainLoss);
        _eval_losses.Add(evalLoss);
        _eval_accuracies.Add(evalAcc);
    }
}
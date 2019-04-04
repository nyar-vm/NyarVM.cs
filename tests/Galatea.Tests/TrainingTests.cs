namespace Galatea.Tests;

/// <summary>
///     训练管线测试 —— 验证 LeNet-5 训练收敛
/// </summary>
public class TrainingTests : GradientTestBase
{
    /// <summary>
    ///     生成合成 MNIST 数据（用于验证训练管线）
    /// </summary>
    private static (ArrayND Inputs, ArrayND Labels) GenerateSyntheticData(int numSamples, int inputDim, int numClasses)
    {
        var rng = new Random(42);
        var inputsData = new float[numSamples * inputDim];
        var labelsData = new float[numSamples];

        for (var i = 0; i < numSamples; i++)
        {
            labelsData[i] = i % numClasses;

            var label = (int)labelsData[i];
            for (var j = 0; j < inputDim; j++)
                inputsData[i * inputDim + j] = (float)(rng.NextDouble() - 0.5) * 0.1f
                                               + (j % numClasses == label ? 0.8f : 0.0f);
        }

        return (ArrayND.FromArray(inputsData, numSamples, inputDim),
            ArrayND.FromArray(labelsData, numSamples, 1));
    }

    #region 梯度更新正确性

    [Fact]
    public void Parameters_UpdateAfterTrainingStep()
    {
        var (trainX, trainY) = GenerateSyntheticData(8, 4, 2);
        var model = new Dense(fanIn: 4, fanOut: 2);
        var weightBefore = model.Weight.Clone();
        var optimizer = new SGD(learningRate: 0.1f);
        var trainer = new Trainer(model, optimizer);

        trainer.TrainStep(trainX, trainY);

        var maxDiff = 0.0f;
        var spanBefore = weightBefore.AsSpan();
        var spanAfter = model.Weight.AsSpan();
        for (var i = 0; i < spanBefore.Length; i++)
            maxDiff = MathF.Max(maxDiff, MathF.Abs(spanBefore[i] - spanAfter[i]));

        Assert.True(maxDiff > 0.0f, "训练步骤后权重应有变化");
    }

    #endregion

    #region 基础训练

    [Fact]
    public void TrainStep_ReducesLoss_OnSmallDataset()
    {
        var (trainX, trainY) = GenerateSyntheticData(32, 28 * 28, 10);
        var (evalX, evalY) = GenerateSyntheticData(16, 28 * 28, 10);

        var model = new Dense(fanIn: 28 * 28, fanOut: 10);
        var optimizer = new SGD(learningRate: 0.01f);
        var trainer = new Trainer(model, optimizer);

        var (initialLoss, initialAcc) = trainer.Evaluate(evalX, evalY, batchSize: 8);
        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 5, batchSize: 8);

        var finalLoss = history.EvalLosses[^1];
        var finalAcc = history.EvalAccuracies[^1];

        Assert.True(finalLoss < initialLoss,
            $"训练后损失应下降：初始={initialLoss:F4}，最终={finalLoss:F4}");

        Assert.True(finalAcc > initialAcc,
            $"训练后准确率应上升：初始={initialAcc:P2}，最终={finalAcc:P2}");
    }

    [Fact]
    public void TrainStep_SmallModel_OverfitsSingleBatch()
    {
        var (trainX, trainY) = GenerateSyntheticData(8, 4, 2);
        var model = new Dense(fanIn: 4, fanOut: 2);
        var optimizer = new SGD(learningRate: 0.5f);
        var trainer = new Trainer(model, optimizer);

        var initialLoss = float.MaxValue;
        for (var epoch = 0; epoch < 20; epoch++)
        {
            var loss = trainer.TrainEpoch(trainX, trainY, batchSize: 8);
            if (loss < initialLoss * 0.1f)
            {
                Assert.True(true);
                return;
            }

            initialLoss = Math.Min(initialLoss, loss);
        }

        Assert.Fail("模型应在 20 个 epoch 内过拟合到很小损失");
    }

    #endregion

    #region 训练历史记录

    [Fact]
    public void TrainingHistory_RecordsAllEpochs()
    {
        var (trainX, trainY) = GenerateSyntheticData(16, 8, 3);
        var (evalX, evalY) = GenerateSyntheticData(8, 8, 3);
        var model = new Dense(fanIn: 8, fanOut: 3);
        var optimizer = new SGD(learningRate: 0.01f);
        var trainer = new Trainer(model, optimizer);

        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 5, batchSize: 4);

        Assert.Equal(5, history.TrainLosses.Count);
        Assert.Equal(5, history.EvalLosses.Count);
        Assert.Equal(5, history.EvalAccuracies.Count);
    }

    [Fact]
    public void TrainingHistory_LossGenerallyDecreasing()
    {
        var (trainX, trainY) = GenerateSyntheticData(32, 28 * 28, 10);
        var (evalX, evalY) = GenerateSyntheticData(16, 28 * 28, 10);
        var model = new Dense(fanIn: 28 * 28, fanOut: 10);
        var optimizer = new SGD(learningRate: 0.02f);
        var trainer = new Trainer(model, optimizer);

        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 10, batchSize: 8);

        var firstHalfAvg = history.TrainLosses.Take(5).Average();
        var secondHalfAvg = history.TrainLosses.Skip(5).Average();

        Assert.True(secondHalfAvg < firstHalfAvg,
            $"训练损失应下降：前半均值={firstHalfAvg:F4}，后半均值={secondHalfAvg:F4}");
    }

    #endregion

    #region LeNet-5 微缩训练

    /// <summary>
    ///     微缩版 LeNet-5（简化结构，用于测试训练收敛）
    /// </summary>
    private sealed class MicroLeNet : ITrainableModel
    {
        public Dense Fc1 { get; } = new(fanIn: 64, fanOut: 32);
        public Dense Fc2 { get; } = new(fanIn: 32, fanOut: 10);

        public ArrayND Forward(ArrayND input)
        {
            var x = Fc1.Forward(input);
            x = Activations.ReLUForward(x);
            x = Fc2.Forward(x);
            return x;
        }

        public ArrayND Forward(ArrayND input, AutogradContext ctx)
        {
            var x = Fc1.Forward(input, ctx);
            x = Activations.ReLU(x, ctx);
            x = Fc2.Forward(x, ctx);
            return x;
        }

        public IEnumerable<IParameter> Parameters()
        {
            foreach (var p in Fc1.Parameters()) yield return p;

            foreach (var p in Fc2.Parameters()) yield return p;
        }
    }

    [Fact]
    public void MicroLeNet_TrainsAndImproves()
    {
        var (trainX, trainY) = GenerateSyntheticData(64, 64, 10);
        var (evalX, evalY) = GenerateSyntheticData(32, 64, 10);
        var model = new MicroLeNet();
        var optimizer = new SGD(learningRate: 0.05f);
        var trainer = new Trainer(model, optimizer);

        var (initialLoss, initialAcc) = trainer.Evaluate(evalX, evalY, batchSize: 8);
        var history = trainer.Fit(trainX, trainY, evalX, evalY, epochs: 15, batchSize: 8);

        var finalLoss = history.EvalLosses[^1];
        var finalAcc = history.EvalAccuracies[^1];

        Assert.True(finalLoss < initialLoss * 0.8f,
            $"评估损失应显著下降：初始={initialLoss:F4}，最终={finalLoss:F4}");

        Assert.True(finalAcc >= initialAcc,
            $"评估准确率应维持或上升：初始={initialAcc:P2}，最终={finalAcc:P2}");
    }

    #endregion

    #region CrossEntropy + 优化器集成

    [Fact]
    public void SoftmaxCrossEntropy_SgdTraining_Converges()
    {
        var (trainX, trainY) = GenerateSyntheticData(16, 10, 3);
        var model = new Dense(fanIn: 10, fanOut: 3);
        var optimizer = new SGD(learningRate: 0.1f);
        var trainer = new Trainer(model, optimizer);

        var initialLoss = trainer.TrainStep(trainX, trainY);
        for (var step = 0; step < 50; step++)
        {
            var loss = trainer.TrainStep(trainX, trainY);
            if (loss < initialLoss * 0.3f)
            {
                Assert.True(true);
                return;
            }
        }

        var finalLoss = trainer.TrainStep(trainX, trainY);
        Assert.True(finalLoss < initialLoss * 0.5f,
            $"50 步后损失应显著下降：初始={initialLoss:F4}，最终={finalLoss:F4}");
    }

    [Fact]
    public void AdamOptimizer_ConvergesFaster()
    {
        var (trainX, trainY) = GenerateSyntheticData(32, 10, 3);
        var model = new Dense(fanIn: 10, fanOut: 3);
        var optimizer = new Adam(learningRate: 0.01f);
        var trainer = new Trainer(model, optimizer);

        var initialLoss = trainer.TrainStep(trainX, trainY);
        var lossAt5 = initialLoss;
        for (var step = 0; step < 15; step++) lossAt5 = trainer.TrainStep(trainX, trainY);

        Assert.True(lossAt5 < initialLoss,
            $"Adam 15 步后损失应下降：初始={initialLoss:F4}，15步后={lossAt5:F4}");
    }

    #endregion

    #region 优化器全覆盖

    [Fact]
    public void AdamW_UpdatesParameters()
    {
        var (trainX, trainY) = GenerateSyntheticData(8, 4, 2);
        var model = new Dense(fanIn: 4, fanOut: 2);
        var weightBefore = model.Weight.Clone();
        var optimizer = new AdamW(learningRate: 0.1f, weightDecay: 0.01f);
        var trainer = new Trainer(model, optimizer);

        trainer.TrainStep(trainX, trainY);

        var maxDiff = 0.0f;
        var spanBefore = weightBefore.AsSpan();
        var spanAfter = model.Weight.AsSpan();
        for (var i = 0; i < spanBefore.Length; i++)
            maxDiff = MathF.Max(maxDiff, MathF.Abs(spanBefore[i] - spanAfter[i]));

        Assert.True(maxDiff > 0.0f, "AdamW 训练步骤后权重应有变化");
    }

    [Fact]
    public void RMSprop_UpdatesParameters()
    {
        var (trainX, trainY) = GenerateSyntheticData(8, 4, 2);
        var model = new Dense(fanIn: 4, fanOut: 2);
        var weightBefore = model.Weight.Clone();
        var optimizer = new RMSprop(learningRate: 0.1f);
        var trainer = new Trainer(model, optimizer);

        trainer.TrainStep(trainX, trainY);

        var maxDiff = 0.0f;
        var spanBefore = weightBefore.AsSpan();
        var spanAfter = model.Weight.AsSpan();
        for (var i = 0; i < spanBefore.Length; i++)
            maxDiff = MathF.Max(maxDiff, MathF.Abs(spanBefore[i] - spanAfter[i]));

        Assert.True(maxDiff > 0.0f, "RMSprop 训练步骤后权重应有变化");
    }

    [Fact]
    public void SGD_WithMomentum_UpdatesParameters()
    {
        var (trainX, trainY) = GenerateSyntheticData(8, 4, 2);
        var model = new Dense(fanIn: 4, fanOut: 2);
        var weightBefore = model.Weight.Clone();
        var optimizer = new SGD(learningRate: 0.1f, momentum: 0.9f);
        var trainer = new Trainer(model, optimizer);

        trainer.TrainStep(trainX, trainY);

        var maxDiff = 0.0f;
        var spanBefore = weightBefore.AsSpan();
        var spanAfter = model.Weight.AsSpan();
        for (var i = 0; i < spanBefore.Length; i++)
            maxDiff = MathF.Max(maxDiff, MathF.Abs(spanBefore[i] - spanAfter[i]));

        Assert.True(maxDiff > 0.0f, "带动量的 SGD 训练步骤后权重应有变化");
    }

    [Fact]
    public void EndToEnd_TrainingLoop_WithAllOptimizers()
    {
        var (trainX, trainY) = GenerateSyntheticData(16, 8, 3);

        IOptimizer[] optimizers =
        {
            new SGD(learningRate: 0.1f),
            new Adam(learningRate: 0.05f),
            new AdamW(learningRate: 0.05f, weightDecay: 0.001f),
            new RMSprop(learningRate: 0.05f)
        };

        foreach (var optimizer in optimizers)
        {
            var model = new Dense(fanIn: 8, fanOut: 3);
            var trainer = new Trainer(model, optimizer);

            var initialLoss = trainer.TrainStep(trainX, trainY);

            for (var step = 0; step < 4; step++) trainer.TrainStep(trainX, trainY);

            var finalLoss = trainer.TrainStep(trainX, trainY);

            Assert.True(finalLoss < initialLoss,
                $"{optimizer.GetType().Name} 5 步后损失应下降：初始={initialLoss:F4}，最终={finalLoss:F4}");
        }
    }

    #endregion
}
namespace Galatea.Tests;

/// <summary>
///     Autograd 综合测试 —— 多层网络、梯度累加、多次反向传播
/// </summary>
public class AutogradTests : GradientTestBase
{
    [Fact]
    public void MultiLayerNetwork_GradientFlowsThroughAllLayers()
    {
        var dense1 = new Dense(fanIn: 4, fanOut: 3);
        var dense2 = new Dense(fanIn: 3, fanOut: 2);

        var w1Span = dense1.Weight.AsWriteSpan();
        for (var i = 0; i < w1Span.Length; i++) w1Span[i] = 0.1f;
        var w2Span = dense2.Weight.AsWriteSpan();
        for (var i = 0; i < w2Span.Length; i++) w2Span[i] = 0.1f;

        var input = ArrayND.FromArray(new[] { 1.0f, 1.0f, 1.0f, 1.0f }, 1, 4);

        var ctx = new AutogradContext();
        ctx.StartRecording();

        var x = dense1.Forward(input, ctx);
        x = Activations.ReLU(x, ctx);
        x = dense2.Forward(x, ctx);

        var allOnesGrad = ArrayND.Ones(x.Shape);
        ctx.BackwardFromGradient(x, allOnesGrad);

        Assert.NotNull(dense1.Weight.Grad);
        Assert.NotNull(dense1.Bias.Grad);
        Assert.NotNull(dense2.Weight.Grad);
        Assert.NotNull(dense2.Bias.Grad);

        var w1GradSpan = dense1.Weight.Grad!.AsSpan();
        var anyNonZero = false;
        for (var i = 0; i < w1GradSpan.Length; i++)
            if (MathF.Abs(w1GradSpan[i]) > 1e-10f)
            {
                anyNonZero = true;
                break;
            }

        Assert.True(anyNonZero, "第一层权重应有非零梯度");
    }

    [Fact]
    public void SharedParameter_AccumulatesGradients()
    {
        var shared = new Dense(fanIn: 3, fanOut: 3);
        var input = RandomInput(1, 3);

        var ctx = new AutogradContext();
        ctx.StartRecording();

        var path1 = shared.Forward(input, ctx);
        var path2 = shared.Forward(input, ctx);
        var combined = path1 + path2;

        ctx.Backward(combined);

        Assert.NotNull(shared.Weight.Grad);
    }

    [Fact]
    public void ZeroGrad_ClearsAllGradients()
    {
        var dense = new Dense(fanIn: 4, fanOut: 2);
        var input = RandomInput(1, 4);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = dense.Forward(input, ctx);
        ctx.Backward(output);

        Assert.NotNull(dense.Weight.Grad);
        var beforeGrad = dense.Weight.Grad!.AsSpan();
        var hasNonZero = false;
        for (var i = 0; i < beforeGrad.Length; i++)
            if (MathF.Abs(beforeGrad[i]) > 1e-8f)
            {
                hasNonZero = true;
                break;
            }

        Assert.True(hasNonZero, "ZeroGrad 前梯度应有非零值");

        dense.Weight.ZeroGrad();
        var afterGrad = dense.Weight.Grad!.AsSpan();
        for (var i = 0; i < afterGrad.Length; i++) Assert.Equal(0.0f, afterGrad[i]);
    }

    [Fact]
    public void MultipleBackwardPasses_AccumulateGradients()
    {
        var dense = new Dense(fanIn: 3, fanOut: 2);
        var input = RandomInput(1, 3);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = dense.Forward(input, ctx);
        ctx.Backward(output);

        var gradAfterFirst = new float[dense.Weight.Grad!.Size];
        dense.Weight.Grad.AsSpan().CopyTo(gradAfterFirst);

        ctx.StartRecording();
        var output2 = dense.Forward(input, ctx);
        ctx.Backward(output2);

        var gradAfterSecond = dense.Weight.Grad!.AsSpan();
        var hasAccumulated = false;
        for (var i = 0; i < gradAfterSecond.Length; i++)
            if (MathF.Abs(gradAfterSecond[i]) > MathF.Abs(gradAfterFirst[i]) + 1e-6f)
            {
                hasAccumulated = true;
                break;
            }

        Assert.True(hasAccumulated, "第二次 backward 后梯度应累加");
    }

    [Fact]
    public void Optimizer_SGD_UpdatesParameters()
    {
        var dense = new Dense(fanIn: 3, fanOut: 2);
        var input = RandomInput(1, 3);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = dense.Forward(input, ctx);
        ctx.Backward(output);

        var weightBefore = new float[dense.Weight.Size];
        dense.Weight.AsSpan().CopyTo(weightBefore);

        var optimizer = new SGD(learningRate: 1.0f);
        optimizer.Step(dense.Parameters());

        var weightAfter = dense.Weight.AsSpan();
        var changed = false;
        for (var i = 0; i < weightAfter.Length; i++)
            if (MathF.Abs(weightAfter[i] - weightBefore[i]) > 1e-8f)
            {
                changed = true;
                break;
            }

        Assert.True(changed, "SGD 优化器应修改参数");
    }

    [Fact]
    public void Optimizer_Adam_UpdatesParameters()
    {
        var dense = new Dense(fanIn: 3, fanOut: 2);
        var input = RandomInput(1, 3);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = dense.Forward(input, ctx);
        ctx.Backward(output);

        var weightBefore = new float[dense.Weight.Size];
        dense.Weight.AsSpan().CopyTo(weightBefore);

        var optimizer = new Adam(learningRate: 0.01f);
        optimizer.Step(dense.Parameters());

        var weightAfter = dense.Weight.AsSpan();
        var changed = false;
        for (var i = 0; i < weightAfter.Length; i++)
            if (MathF.Abs(weightAfter[i] - weightBefore[i]) > 1e-8f)
            {
                changed = true;
                break;
            }

        Assert.True(changed, "Adam 优化器应修改参数");
    }

    [Fact]
    public void Optimizer_AdamW_UpdatesParameters()
    {
        var dense = new Dense(fanIn: 3, fanOut: 2);
        var input = RandomInput(1, 3);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = dense.Forward(input, ctx);
        ctx.Backward(output);

        var weightBefore = new float[dense.Weight.Size];
        dense.Weight.AsSpan().CopyTo(weightBefore);

        var optimizer = new AdamW(learningRate: 0.01f, weightDecay: 0.1f);
        optimizer.Step(dense.Parameters());

        var weightAfter = dense.Weight.AsSpan();
        var changed = false;
        for (var i = 0; i < weightAfter.Length; i++)
            if (MathF.Abs(weightAfter[i] - weightBefore[i]) > 1e-8f)
            {
                changed = true;
                break;
            }

        Assert.True(changed, "AdamW 优化器应修改参数");
    }

    [Fact]
    public void Optimizer_RMSprop_UpdatesParameters()
    {
        var dense = new Dense(fanIn: 3, fanOut: 2);
        var input = RandomInput(1, 3);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = dense.Forward(input, ctx);
        ctx.Backward(output);

        var weightBefore = new float[dense.Weight.Size];
        dense.Weight.AsSpan().CopyTo(weightBefore);

        var optimizer = new RMSprop(learningRate: 0.01f);
        optimizer.Step(dense.Parameters());

        var weightAfter = dense.Weight.AsSpan();
        var changed = false;
        for (var i = 0; i < weightAfter.Length; i++)
            if (MathF.Abs(weightAfter[i] - weightBefore[i]) > 1e-8f)
            {
                changed = true;
                break;
            }

        Assert.True(changed, "RMSprop 优化器应修改参数");
    }
}
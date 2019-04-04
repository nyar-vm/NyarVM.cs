namespace Galatea.Tests;

/// <summary>
///     Conflux 算子融合测试 —— 验证融合后输出与顺序计算一致
/// </summary>
public class ConfluxFusionTests : GradientTestBase
{
    #region F1: Conv2D + BatchNorm + ReLU

    [Fact]
    public void FuseConv2DBatchNormReLU_ProducesSameOutput()
    {
        var conv = new Conv2D(inChannels: 2, outChannels: 2, kernelSize: 3, padding: 0);
        var input = RandomInput(1, 2 * 5 * 5);

        var (convOut, outH, outW) = conv.Forward(input, inH: 5, inW: 5);
        var bn = new BatchNorm(convOut.Size / convOut.Shape[0]);
        var bnOut = bn.Forward(convOut);
        var sequential = Activations.ReLUForward(bnOut);

        var (fused, fH, fW) = ConfluxGraphFusion.FuseConv2DBatchNormReLU(input, conv, bn, 5, 5);

        Assert.Equal(outH, fH);
        Assert.Equal(outW, fW);
        var error = MaxAbsoluteError(sequential, fused);
        Assert.True(error < GradientTolerance, $"F1 融合误差 {error} 超过容差");
    }

    #endregion

    #region F2: Dense + ReLU

    [Fact]
    public void FuseDenseReLU_ProducesSameOutput()
    {
        var dense = new Dense(fanIn: 4, fanOut: 3);
        var input = RandomInput(2, 4);

        var sequential = Activations.ReLUForward(dense.Forward(input));
        var fused = ConfluxGraphFusion.FuseDenseReLU(input, dense);

        var error = MaxAbsoluteError(sequential, fused);
        Assert.True(error < GradientTolerance, $"F2 融合误差 {error} 超过容差");
    }

    #endregion

    #region F3: Dense + Dropout

    [Fact]
    public void FuseDenseDropout_ProducesSameOutput()
    {
        var dense = new Dense(fanIn: 4, fanOut: 3);
        var dropout = new Dropout(0.0f);
        var input = RandomInput(2, 4);

        var sequential = dropout.Forward(dense.Forward(input));
        var fused = ConfluxGraphFusion.FuseDenseDropout(input, dense, dropout);

        var error = MaxAbsoluteError(sequential, fused);
        Assert.True(error < GradientTolerance, $"F3 融合误差 {error} 超过容差");
    }

    #endregion

    #region F4: Conv2D + SiLU

    [Fact]
    public void FuseConv2DSiLU_ProducesSameOutput()
    {
        var conv = new Conv2D(inChannels: 1, outChannels: 2, kernelSize: 3, padding: 0);
        var input = RandomInput(1, 1 * 5 * 5);

        var (convOut, outH, outW) = conv.Forward(input, inH: 5, inW: 5);
        var sequential = Activations.SiLUForward(convOut);

        var (fused, fH, fW) = ConfluxGraphFusion.FuseConv2DSiLU(input, conv, 5, 5);

        Assert.Equal(outH, fH);
        Assert.Equal(outW, fW);
        var error = MaxAbsoluteError(sequential, fused);
        Assert.True(error < GradientTolerance, $"F4 融合误差 {error} 超过容差");
    }

    #endregion

    #region F5: Dense + BatchNorm (Weight Folding)

    [Fact]
    public void FuseDenseBatchNorm_ProducesSameOutput()
    {
        var dense = new Dense(fanIn: 4, fanOut: 3);
        var bn = new BatchNorm(3);
        var input = RandomInput(2, 4);

        var denseOut = dense.Forward(input);
        var sequential = bn.Forward(denseOut);
        var fused = ConfluxGraphFusion.FuseDenseBatchNorm(input, dense, bn);

        var error = MaxAbsoluteError(sequential, fused);
        Assert.True(error < GradientTolerance, $"F5 融合误差 {error} 超过容差");
    }

    #endregion

    #region Inspector

    [Fact]
    public void ConfluxFusionInspector_RecordsFusionsCorrectly()
    {
        var inspector = new ConfluxFusionInspector();

        inspector.RecordFusion("Dense+ReLU");
        inspector.RecordFusion("Dense+ReLU");
        inspector.RecordFusion("Conv2D+BatchNorm+ReLU");

        Assert.Equal(3, inspector.TotalFusionsApplied);
        Assert.Equal(2, inspector.UniqueFusionRules);

        var stats = inspector.GetFusionStats();
        Assert.Equal(2, stats["Dense+ReLU"]);
        Assert.Equal(1, stats["Conv2D+BatchNorm+ReLU"]);
    }

    #endregion

    [Fact]
    public void AvailableFusions_ReturnsAllFiveRules()
    {
        var fusions = ConfluxGraphFusion.GetAvailableFusions();
        Assert.Equal(5, fusions.Length);
        Assert.Contains("Conv2D+BatchNorm+ReLU", fusions);
        Assert.Contains("Dense+ReLU", fusions);
        Assert.Contains("Dense+Dropout", fusions);
        Assert.Contains("Conv2D+SiLU", fusions);
        Assert.Contains("Dense+BatchNorm", fusions);
    }
}
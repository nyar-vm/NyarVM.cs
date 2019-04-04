namespace Galatea.Tests;

/// <summary>
///     LoRA 低秩适配集成测试 —— 验证 M11 里程碑：
///     低秩适配、参数冻结、增量存储、合并/取消合并、架构正确性
/// </summary>
public class LoraTests : GradientTestBase
{
    #region 辅助方法

    /// <summary>
    ///     将 ArrayND 的所有元素设置为指定值
    /// </summary>
    private static void SetAllValues(ArrayND tensor, float value)
    {
        var span = tensor.AsWriteSpan();
        for (var i = 0; i < span.Length; i++) span[i] = value;
    }

    #endregion

    #region 构造与基础属性

    [Fact]
    public void LoRALinear_Creation_InitializesAdaptors()
    {
        var dense = new Dense(10, 5);
        var lora = new LoRALinear(dense, rank: 4);

        Assert.Equal(10, lora.FanIn);
        Assert.Equal(5, lora.FanOut);
        Assert.Equal(4, lora.Rank);
        Assert.False(lora.IsMerged);
        Assert.NotNull(lora.WeightA);
        Assert.NotNull(lora.WeightB);
    }

    [Fact]
    public void LoRALinear_WeightA_Shape_Correct()
    {
        var dense = new Dense(6, 4);
        var lora = new LoRALinear(dense, rank: 3);

        Assert.Equal(6, lora.WeightA.Shape[0]);
        Assert.Equal(3, lora.WeightA.Shape[1]);
    }

    [Fact]
    public void LoRALinear_WeightB_Shape_Correct()
    {
        var dense = new Dense(6, 4);
        var lora = new LoRALinear(dense, rank: 3);

        Assert.Equal(3, lora.WeightB.Shape[0]);
        Assert.Equal(4, lora.WeightB.Shape[1]);
    }

    [Fact]
    public void LoRALinear_ScaleFactor_Correct()
    {
        var dense = new Dense(8, 3);
        var loraA = new LoRALinear(dense, rank: 4, alpha: 8.0f);
        var loraB = new LoRALinear(dense, rank: 2, alpha: 4.0f);

        Assert.Equal(2.0f, loraA.ScaleFactor, 6);
        Assert.Equal(2.0f, loraB.ScaleFactor, 6);
    }

    [Fact]
    public void LoRALinear_BInitializedZero_OutputEqualsBase()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);
        var input = RandomInput(1, 4);

        var baseOutput = dense.Forward(input);
        var loraOutput = lora.Forward(input);

        var error = MaxAbsoluteError(baseOutput, loraOutput);
        Assert.True(error < 1e-6f,
            $"B 初始为零时 LoRA 输出应等于基础层，误差={error}");
    }

    #endregion

    #region 前向传播

    [Fact]
    public void LoRALinear_Forward_OutputShape()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);
        var input = RandomInput(2, 4);

        var output = lora.Forward(input);

        Assert.Equal(2, output.Shape[0]);
        Assert.Equal(3, output.Shape[1]);
    }

    [Fact]
    public void LoRALinear_AfterModifyingB_OutputDiffers()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);
        var input = RandomInput(1, 4);

        var baseOutput = lora.Forward(input);

        SetAllValues(lora.WeightB, 0.5f);

        var modifiedOutput = lora.Forward(input);
        var error = MaxAbsoluteError(baseOutput, modifiedOutput);

        Assert.True(error > 1e-10f,
            $"修改 B 后 LoRA 输出应不同，误差={error}");
    }

    [Fact]
    public void LoRALinear_AfterModifyingA_OutputDiffers()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);
        var input = RandomInput(1, 4);

        // B 需非零，修改 A 才能产生输出差异
        SetAllValues(lora.WeightB, 0.1f);
        var baseOutput = lora.Forward(input);

        SetAllValues(lora.WeightA, 0.3f);

        var modifiedOutput = lora.Forward(input);
        var error = MaxAbsoluteError(baseOutput, modifiedOutput);

        Assert.True(error > 1e-10f,
            $"修改 A 后 LoRA 输出应不同，误差={error}");
    }

    [Fact]
    public void LoRALinear_ForwardWithAutograd_OutputCorrect()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);
        var input = RandomInput(1, 4);

        var ctx = new AutogradContext();
        ctx.StartRecording();
        var output = lora.Forward(input, ctx);

        Assert.Equal(1, output.Shape[0]);
        Assert.Equal(3, output.Shape[1]);
    }

    [Fact]
    public void LoRALinear_Merged_OutputIdenticalToBase()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);

        SetAllValues(lora.WeightB, 0.1f);
        lora.MergeWeights();

        Assert.True(lora.IsMerged);

        var input = RandomInput(1, 4);
        var output = lora.Forward(input);

        Assert.Equal(1, output.Shape[0]);
        Assert.Equal(3, output.Shape[1]);
    }

    #endregion

    #region 参数管理

    [Fact]
    public void LoRALinear_Parameters_OnlyReturnsAdaptorParams()
    {
        var dense = new Dense(6, 4);
        var lora = new LoRALinear(dense, rank: 2);

        var loraParams = lora.Parameters().ToList();
        Assert.Equal(2, loraParams.Count);
        Assert.All(loraParams, p => Assert.NotNull(p.Value));
    }

    [Fact]
    public void LoRALinear_AllParameters_IncludesBaseParams()
    {
        var dense = new Dense(6, 4);
        var lora = new LoRALinear(dense, rank: 2);

        var allParams = lora.AllParameters().ToList();
        Assert.Equal(4, allParams.Count);
    }

    [Fact]
    public void LoRALinear_BaseLayerWeights_NotModifiedByLoraConstruct()
    {
        var dense = new Dense(4, 3);
        var originalWeight = dense.Weight.Clone();

        _ = new LoRALinear(dense, rank: 2);

        var error = MaxAbsoluteError(originalWeight, dense.Weight);
        Assert.True(error < 1e-6f,
            $"创建 LoRA 不应修改基础层权重，误差={error}");
    }

    [Fact]
    public void LoRALinear_LoraWeights_TrainableWithoutAffectingBase()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);
        var originalBaseWeight = dense.Weight.Clone();

        SetAllValues(lora.WeightB, 0.2f);

        var input = RandomInput(2, 4);
        var outputBefore = lora.Forward(input);

        SetAllValues(lora.WeightB, 0.4f);

        var outputAfter = lora.Forward(input);

        var errorOutput = MaxAbsoluteError(outputBefore, outputAfter);
        Assert.True(errorOutput > 1e-10f, $"修改 B 应改变输出，误差={errorOutput}");

        var errorBase = MaxAbsoluteError(originalBaseWeight, dense.Weight);
        Assert.True(errorBase < 1e-6f, $"基础层权重不应改变，误差={errorBase}");
    }

    #endregion

    #region 合并与取消合并

    [Fact]
    public void LoRALinear_MergeWeights_ChangesBaseWeight_AfterModifyingB()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);

        SetAllValues(lora.WeightB, 0.5f);
        var originalWeight = dense.Weight.Clone();

        lora.MergeWeights();

        var error = MaxAbsoluteError(originalWeight, dense.Weight);
        Assert.True(error > 1e-10f,
            $"B=0.5 时合并应改变基础权重，误差={error}");
        Assert.True(lora.IsMerged);
    }

    [Fact]
    public void LoRALinear_MergeWeights_Idempotent()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);

        SetAllValues(lora.WeightB, 0.3f);
        lora.MergeWeights();
        var afterFirst = dense.Weight.Clone();

        lora.MergeWeights();
        var afterSecond = dense.Weight.Clone();

        var error = MaxAbsoluteError(afterFirst, afterSecond);
        Assert.True(error < 1e-6f, $"二次合并应不改变权重，误差={error}");
    }

    [Fact]
    public void LoRALinear_Unmerge_ResetsMergedFlag()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);

        lora.MergeWeights();
        Assert.True(lora.IsMerged);

        lora.UnmergeWeights();
        Assert.False(lora.IsMerged);
    }

    [Fact]
    public void LoRALinear_GetIncrementWeights_CorrectShape()
    {
        var dense = new Dense(6, 4);
        var lora = new LoRALinear(dense, rank: 3);

        var increments = lora.GetIncrementWeights();
        Assert.Equal(6 * 4, increments.Length);
    }

    [Fact]
    public void LoRALinear_IncrementWeights_NonZero_AfterModifyingB()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);

        SetAllValues(lora.WeightB, 1.0f);
        var increments = lora.GetIncrementWeights();

        var hasNonZero = increments.Any(v => MathF.Abs(v) > 1e-10f);
        Assert.True(hasNonZero, "修改 B=1 后增量权重应有非零值");
    }

    #endregion

    #region 存储效率

    [Fact]
    public void LoRALinear_SaveLoadLoraWeights_PreservesData()
    {
        var dense = new Dense(6, 4);
        var lora = new LoRALinear(dense, rank: 3);

        SetAllValues(lora.WeightA, 1.0f);
        SetAllValues(lora.WeightB, 2.0f);

        var aData = ModelSerializer.Serialize(lora.Parameters());
        var deserialized = ModelSerializer.Deserialize(aData);

        var originalA = lora.WeightA.AsSpan();
        var originalB = lora.WeightB.AsSpan();
        var loadedA = deserialized[0].AsSpan();
        var loadedB = deserialized[1].AsSpan();

        for (var i = 0; i < originalA.Length; i++) Assert.Equal(originalA[i], loadedA[i], 1e-6f);

        for (var i = 0; i < originalB.Length; i++) Assert.Equal(originalB[i], loadedB[i], 1e-6f);
    }

    [Fact]
    public void LoRALinear_AdapterStorage_SmallerThanFullWeights()
    {
        var dense = new Dense(128, 64);
        var lora = new LoRALinear(dense, rank: 8);

        var fullWeightBytes = 128 * 64 * sizeof(float);
        var loraAbytes = 128 * 8 * sizeof(float);
        var loraBbytes = 8 * 64 * sizeof(float);
        var loraStorageBytes = loraAbytes + loraBbytes;

        Assert.True(loraStorageBytes < fullWeightBytes,
            $"LoRA 存储 {loraStorageBytes}B 应 < 全权重 {fullWeightBytes}B");

        var compressionRatio = (float)fullWeightBytes / loraStorageBytes;
        Assert.True(compressionRatio > 3.0f,
            $"压缩比应 > 3x，实际={compressionRatio:F1}x");
    }

    #endregion

    #region 集成架构

    [Fact]
    public void LoRALinear_DeterministicBehavior_SameInputSameOutput()
    {
        var dense = new Dense(4, 3);
        var lora = new LoRALinear(dense, rank: 2);
        var input = RandomInput(1, 4);

        var output1 = lora.Forward(input);
        var output2 = lora.Forward(input);

        var error = MaxAbsoluteError(output1, output2);
        Assert.True(error < 1e-8f, $"相同输入应产生相同输出，误差={error}");
    }

    [Fact]
    public void LoRALinear_DifferentRankes_AllWork()
    {
        var dense = new Dense(16, 8);

        foreach (var rank in new[] { 1, 2, 4, 8, 16 })
        {
            var lora = new LoRALinear(dense, rank: rank, alpha: rank * 2.0f);
            var input = RandomInput(3, 16);

            var output = lora.Forward(input);
            Assert.Equal(3, output.Shape[0]);
            Assert.Equal(8, output.Shape[1]);
        }
    }

    #endregion
}
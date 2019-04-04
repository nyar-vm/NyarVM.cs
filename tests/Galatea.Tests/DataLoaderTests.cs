namespace Galatea.Tests;

using DataLoader = Galatea.Data.DataLoader;

/// <summary>
///     DataLoader 集成测试 —— 验证 M10 里程碑：
///     数据分批、乱序、epoch 迭代、预取
/// </summary>
public class DataLoaderTests : GradientTestBase
{
    #region Dataset

    [Fact]
    public void InMemoryDataset_Count_MatchesInput()
    {
        var (inputs, labels) = CreateSyntheticData(100, 10);
        var ds = new InMemoryDataset(inputs, labels, 10);

        Assert.Equal(100, ds.Count);
        Assert.Equal(10, ds.InputSize);
        Assert.Equal(10, ds.OutputSize);
    }

    [Fact]
    public void InMemoryDataset_GetBatch_ReturnsCorrectShape()
    {
        var (inputs, labels) = CreateSyntheticData(100, 5);
        var ds = new InMemoryDataset(inputs, labels, 5);

        var (batchInputs, batchLabels) = ds.GetBatch(new[] { 0, 1, 2 });

        Assert.Equal(3, batchInputs.Shape[0]);
        Assert.Equal(5, batchInputs.Shape[1]);
        Assert.Equal(3, batchLabels.Shape[0]);
        Assert.Equal(1, batchLabels.Shape[1]);
    }

    [Fact]
    public void InMemoryDataset_GetBatch_PreservesData()
    {
        var (inputs, labels) = CreateSyntheticData(10, 3);
        var ds = new InMemoryDataset(inputs, labels, 3);

        var (batchInputs, batchLabels) = ds.GetBatch(new[] { 0 });
        var allIn = batchInputs.AsSpan();
        var allLabel = batchLabels.AsSpan();

        var firstSample = inputs.AsSpan();
        Assert.Equal(firstSample[0], allIn[0], 1e-6f);
        Assert.Equal(firstSample[1], allIn[1], 1e-6f);
        Assert.Equal(firstSample[2], allIn[2], 1e-6f);
    }

    [Fact]
    public void InMemoryDataset_ShapeMismatch_Throws()
    {
        var inputs = RandomInput(10, 4);
        var labels = RandomInput(8, 1);

        Assert.Throws<ArgumentException>(() =>
            new InMemoryDataset(inputs, labels, 4));
    }

    #endregion

    #region DataLoader 基本分批

    [Fact]
    public void DataLoader_ExactDivision_BatchCount()
    {
        var ds = CreateDataset(100, 8);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 20,
            Shuffle = false
        });

        Assert.Equal(5, loader.BatchCount);

        for (var b = 0; b < loader.Batches.Count; b++) Assert.Equal(20, loader.Batches[b].Inputs.Shape[0]);
    }

    [Fact]
    public void DataLoader_LastBatch_Partial()
    {
        var ds = CreateDataset(100, 8);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 30,
            Shuffle = false,
            DropLast = false
        });

        Assert.Equal(4, loader.BatchCount);
        Assert.Equal(30, loader.Batches[0].Inputs.Shape[0]);
        Assert.Equal(30, loader.Batches[1].Inputs.Shape[0]);
        Assert.Equal(30, loader.Batches[2].Inputs.Shape[0]);
        Assert.Equal(10, loader.Batches[3].Inputs.Shape[0]);
    }

    [Fact]
    public void DataLoader_DropLast_RemovesPartial()
    {
        var ds = CreateDataset(100, 8);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 30,
            Shuffle = false,
            DropLast = true
        });

        Assert.Equal(3, loader.BatchCount);
    }

    [Fact]
    public void DataLoader_NoShuffle_Sequential()
    {
        var ds = CreateDataset(20, 4);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 10,
            Shuffle = false
        });

        // 不 shuffle 时，第一个批次的第一个样本应该是样本 0 的内容
        var batch0Sample0 = loader.Batches[0].Inputs.AsSpan();
        var expected = ds.GetBatch(new[] { 0 }).Inputs.AsSpan();

        for (var i = 0; i < 4; i++) Assert.Equal(expected[i], batch0Sample0[i], 1e-6f);
    }

    [Fact]
    public void DataLoader_Shuffle_DifferentOrder()
    {
        var ds = CreateDataset(100, 4);
        var loader1 = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 20,
            Shuffle = true
        });

        var loader2 = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 20,
            Shuffle = true
        });

        // 两个 loaders 的 shuffles 顺序应该不同（基于 epoch）
        // 获取每个 loader 首个批次中首个样本的第一维
        var l1FirstVal = loader1.Batches[0].Inputs.AsSpan()[0];
        var l2FirstVal = loader2.Batches[0].Inputs.AsSpan()[0];

        // epoch 不同的 shuffle 几乎一定产生不同顺序
        // 种子为 42+0 vs 42+0，相同 seed... 所以通过 NextEpoch 来产生差异
        loader1.NextEpoch();
        var l1E2FirstVal = loader1.Batches[0].Inputs.AsSpan()[0];

        Assert.True(l1FirstVal != l1E2FirstVal,
            "不同 epoch 应产生不同顺序");
    }

    [Fact]
    public void DataLoader_NextEpoch_IncrementsEpoch()
    {
        var ds = CreateDataset(64, 4);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 16,
            Shuffle = true
        });

        Assert.Equal(1, loader.Epoch);

        loader.NextEpoch();
        Assert.Equal(2, loader.Epoch);

        loader.NextEpoch();
        Assert.Equal(3, loader.Epoch);
    }

    [Fact]
    public void DataLoader_Reset_ResetsEpoch()
    {
        var ds = CreateDataset(64, 4);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 16,
            Shuffle = false
        });

        loader.NextEpoch();
        loader.NextEpoch();
        Assert.Equal(3, loader.Epoch);

        loader.Reset();
        Assert.Equal(1, loader.Epoch);
        Assert.Equal(4, loader.BatchCount);
    }

    [Fact]
    public void DataLoader_Reset_RestoresShuffle()
    {
        var ds = CreateDataset(100, 4);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 25,
            Shuffle = true
        });

        var firstBatchOriginal = loader.Batches[0].Inputs.AsSpan()[0];
        loader.NextEpoch();
        loader.NextEpoch();
        loader.Reset();

        var firstBatchAfterReset = loader.Batches[0].Inputs.AsSpan()[0];
        Assert.Equal(firstBatchOriginal, firstBatchAfterReset, 6);
    }

    [Fact]
    public void DataLoader_SingleSample_BatchSize1()
    {
        var ds = CreateDataset(1, 4);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 1,
            Shuffle = false
        });

        Assert.Single(loader.Batches);
        Assert.Equal(1, loader.Batches[0].Inputs.Shape[0]);
    }

    [Fact]
    public void DataLoader_BatchSizeLargerThanDataset_OneBatch()
    {
        var ds = CreateDataset(5, 4);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 100,
            Shuffle = false,
            DropLast = false
        });

        Assert.Equal(1, loader.BatchCount);
        Assert.Equal(5, loader.Batches[0].Inputs.Shape[0]);
    }

    [Fact]
    public void DataLoader_BatchSizeLargerThanDataset_DropLast_Empty()
    {
        var ds = CreateDataset(5, 4);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 100,
            Shuffle = false,
            DropLast = true
        });

        Assert.Equal(0, loader.BatchCount);
    }

    [Fact]
    public void DataLoader_CoversAllSamples_NoOverlap()
    {
        var ds = CreateDataset(50, 3);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 10,
            Shuffle = false,
            DropLast = false
        });

        // 每个样本应该恰好在一个批次中
        var seen = new HashSet<int>();

        for (var b = 0; b < loader.BatchCount; b++)
        {
            var batchInputs = loader.Batches[b].Inputs.AsSpan();
            for (var s = 0; s < loader.Batches[b].Inputs.Shape[0]; s++)
            {
                var sampleInput = new float[3];
                for (var j = 0; j < 3; j++) sampleInput[j] = batchInputs[s * 3 + j];

                // 在原始数据集中找到匹配的样本索引
                int? matchIdx = null;
                var spanAll = ds.GetBatch(Enumerable.Range(0, 50).ToArray()).Inputs.AsSpan();
                for (var k = 0; k < 50; k++)
                {
                    var match = true;
                    for (var j = 0; j < 3; j++)
                        if (MathF.Abs(spanAll[k * 3 + j] - sampleInput[j]) > 1e-6f)
                        {
                            match = false;
                            break;
                        }

                    if (match)
                    {
                        matchIdx = k;
                        break;
                    }
                }

                Assert.True(matchIdx is not null, "每个批次样本应在数据集中存在");
                Assert.True(seen.Add(matchIdx.Value), "每个样本应只出现一次");
            }
        }

        Assert.Equal(50, seen.Count);
    }

    [Fact]
    public void DataLoader_MultipleEpochs_DifferentCounts()
    {
        var ds = CreateDataset(50, 3);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 10,
            Shuffle = false,
            DropLast = false
        });

        var batchCount1 = loader.BatchCount;
        loader.NextEpoch();
        var batchCount2 = loader.BatchCount;

        Assert.Equal(batchCount1, batchCount2);
        Assert.True(batchCount1 == batchCount2, "不同 epoch 批次数量应一致");
    }

    [Fact]
    public void DataLoader_LabelsMatchInputCount()
    {
        var ds = CreateDataset(60, 6);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 15,
            Shuffle = false
        });

        for (var b = 0; b < loader.BatchCount; b++)
        {
            var batch = loader.Batches[b];
            Assert.Equal(batch.Inputs.Shape[0], batch.Labels.Shape[0]);
            Assert.True(batch.Inputs.Shape[0] == batch.Labels.Shape[0],
                $"批次 {b}: 输入标签数量应一致");
        }
    }

    [Fact]
    public void DataLoader_BatchIndex_Sequential()
    {
        var ds = CreateDataset(80, 4);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 10,
            Shuffle = false
        });

        for (var b = 0; b < loader.BatchCount; b++) Assert.Equal(b, loader.Batches[b].BatchIndex);
    }

    [Fact]
    public void DataLoader_TotalSamples_Consistent()
    {
        var ds = CreateDataset(50, 4);
        var loader = new DataLoader(ds, new DataLoaderOptions
        {
            BatchSize = 12,
            Shuffle = false
        });

        for (var b = 0; b < loader.BatchCount; b++) Assert.Equal(50, loader.Batches[b].TotalSamples);
    }

    [Fact]
    public void DataLoader_DefaultOptions_Works()
    {
        var ds = CreateDataset(64, 4);
        var loader = new DataLoader(ds);

        Assert.Equal(32, loader.BatchSize);
        Assert.True(loader.Batches.Count > 0);
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     创建合成数据集
    /// </summary>
    private static (ArrayND Inputs, ArrayND Labels) CreateSyntheticData(
        int numSamples, int numFeatures)
    {
        var rng = new Random(123);
        var inputsData = new float[numSamples * numFeatures];
        var labelsData = new float[numSamples];

        for (var i = 0; i < numSamples; i++)
        {
            labelsData[i] = rng.Next(numFeatures);
            for (var j = 0; j < numFeatures; j++) inputsData[i * numFeatures + j] = (float)(rng.NextDouble() - 0.5);
        }

        return (ArrayND.FromArray(inputsData, numSamples, numFeatures),
            ArrayND.FromArray(labelsData, numSamples, 1));
    }

    /// <summary>
    ///     快速创建 InMemoryDataset
    /// </summary>
    private static InMemoryDataset CreateDataset(int numSamples, int numFeatures)
    {
        var (inputs, labels) = CreateSyntheticData(numSamples, numFeatures);
        return new InMemoryDataset(inputs, labels, numFeatures);
    }

    #endregion
}
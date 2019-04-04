using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using LightDB.Core;
using LightDB.Storage;
using LightDB.Storage.Lsm;
using LightDB.Wal;

namespace LightDB.Benchmarks;

[Config(typeof(BenchmarkConfig))]
[SimpleJob(RuntimeMoniker.Net80, warmupCount: 3, iterationCount: 10)]
[MemoryDiagnoser]
[RankColumn]
public class KvLatencyBenchmark
{
    #region 字段

    private string _tempDir = null!;
    private ILightDatabase _db = null!;
    private LightKey[] _sequentialKeys = null!;
    private LightKey[] _randomKeys = null!;
    private LightValue _testValue;
    private Random _rng = null!;

    #endregion

    #region 参数

    [Params(1000, 10000)]
    public int KeyCount;

    [Params(true, false)]
    public bool UseLsmEngine;

    #endregion

    #region 初始化

    [GlobalSetup]
    public void GlobalSetup()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lightdb_bench_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _sequentialKeys = new LightKey[KeyCount];
        _randomKeys = new LightKey[KeyCount];
        _rng = new Random(42);

        for (var i = 0; i < KeyCount; i++)
        {
            _sequentialKeys[i] = LightKey.FromString($"key_{i:D08}");
            _randomKeys[i] = LightKey.FromString($"key_{_rng.Next(KeyCount):D08}");
        }

        _testValue = LightValue.FromString("benchmark_value_data_1234567890");
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    private ILightDatabase CreateDatabase()
    {
        var builder = new LightDatabaseBuilder()
            .WithStorageOptions(new StorageOptions { Path = _tempDir });

        if (UseLsmEngine)
        {
            var lsmOptions = new LsmOptions
            {
                MemTableMaxSize = 64 * 1024,
                MaxL0Tables = 4,
                BlockSize = 4096,
                BloomFilterExpectedElements = KeyCount,
                BloomFilterFalsePositiveRate = 0.01
            };
            builder.UseStorageEngine(new LsmStorageEngine(_tempDir, lsmOptions));
        }

        return builder.Build();
    }

    [IterationSetup(Targets = [nameof(PutSequential), nameof(PutRandom)])]
    public void IterationSetupForWrites()
    {
        _db = CreateDatabase();
    }

    [IterationCleanup(Targets = [nameof(PutSequential), nameof(PutRandom)])]
    public void IterationCleanupForWrites()
    {
        _db?.Dispose();
    }

    #endregion

    #region 写入延迟基准

    [Benchmark(Description = "顺序写入")]
    [BenchmarkCategory("Write")]
    public void PutSequential()
    {
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Put(_sequentialKeys[i], _testValue).AsTask().Wait();
        }
    }

    [Benchmark(Description = "随机写入")]
    [BenchmarkCategory("Write")]
    public void PutRandom()
    {
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Put(_randomKeys[i], _testValue).AsTask().Wait();
        }
    }

    #endregion

    #region 读取延迟基准

    [GlobalSetup(Targets = [nameof(GetSequential), nameof(GetRandom), nameof(GetMiss)])]
    public void SetupReadData()
    {
        _db = CreateDatabase();
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Put(_sequentialKeys[i], _testValue).AsTask().Wait();
        }
    }

    [GlobalCleanup(Targets = [nameof(GetSequential), nameof(GetRandom), nameof(GetMiss)])]
    public void CleanupReadData()
    {
        _db?.Dispose();
    }

    [Benchmark(Description = "顺序读取")]
    [BenchmarkCategory("Read")]
    public void GetSequential()
    {
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Get<LightValue>(_sequentialKeys[i]).AsTask().Wait();
        }
    }

    [Benchmark(Description = "随机读取")]
    [BenchmarkCategory("Read")]
    public void GetRandom()
    {
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Get<LightValue>(_randomKeys[i]).AsTask().Wait();
        }
    }

    [Benchmark(Description = "未命中读取")]
    [BenchmarkCategory("Read")]
    public void GetMiss()
    {
        var missKey = LightKey.FromString("nonexistent_key");
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Get<LightValue>(missKey).AsTask().Wait();
        }
    }

    #endregion

    #region 删除延迟基准

    [GlobalSetup(Targets = [nameof(DeleteSequential)])]
    public void SetupDeleteData()
    {
        _db = CreateDatabase();
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Put(_sequentialKeys[i], _testValue).AsTask().Wait();
        }
    }

    [GlobalCleanup(Targets = [nameof(DeleteSequential)])]
    public void CleanupDeleteData()
    {
        _db?.Dispose();
    }

    [Benchmark(Description = "顺序删除")]
    [BenchmarkCategory("Delete")]
    public void DeleteSequential()
    {
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Delete(_sequentialKeys[i]).AsTask().Wait();
        }
    }

    #endregion

    #region 缓存命中延迟基准

    [GlobalSetup(Targets = [nameof(GetCacheHit)])]
    public void SetupCacheHitData()
    {
        _db = CreateDatabase();
        _db.Put(_sequentialKeys[0], _testValue).AsTask().Wait();
    }

    [GlobalCleanup(Targets = [nameof(GetCacheHit)])]
    public void CleanupCacheHitData()
    {
        _db?.Dispose();
    }

    [Benchmark(Description = "缓存命中读取")]
    [BenchmarkCategory("Cache")]
    public void GetCacheHit()
    {
        var key = _sequentialKeys[0];
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Get<LightValue>(key).AsTask().Wait();
        }
    }

    #endregion

    #region 范围扫描延迟基准

    [GlobalSetup(Targets = [nameof(RangeScan)])]
    public void SetupRangeScanData()
    {
        _db = CreateDatabase();
        for (var i = 0; i < KeyCount; i++)
        {
            _db.Put(_sequentialKeys[i], _testValue).AsTask().Wait();
        }
    }

    [GlobalCleanup(Targets = [nameof(RangeScan)])]
    public void CleanupRangeScanData()
    {
        _db?.Dispose();
    }

    [Benchmark(Description = "范围扫描")]
    [BenchmarkCategory("Scan")]
    public async Task RangeScan()
    {
        var startKey = _sequentialKeys[0];
        using var cursor = _db.Seek(startKey);
        var entries = await cursor.GetRangeAsync(startKey, _sequentialKeys[Math.Min(99, KeyCount - 1)], 100);
    }

    #endregion
}

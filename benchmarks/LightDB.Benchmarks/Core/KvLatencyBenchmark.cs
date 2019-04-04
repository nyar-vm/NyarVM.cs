using BenchmarkDotNet.Attributes;
using LightDB.Core;

namespace LightDB.Benchmarks.Core;

/// <summary>
///     KV 操作延迟微基准框架
///     覆盖：批量写入、批量读取、混合读写负载
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class KvLatencyBenchmark
{
    private LightDatabase _db = null!;
    private LightKey[] _readKeys = null!;
    private LightKey[] _writeKeys = null!;
    private LightValue[] _values = null!;

    /// <summary>
    ///     批量操作大小
    /// </summary>
    [Params(10, 100, 1000)]
    public int BatchSize { get; set; }

    /// <summary>
    ///     预加载的记录数
    /// </summary>
    [Params(10000)]
    public int PreloadCount { get; set; }

    private int _batchIndex;

    [GlobalSetup]
    public async Task Setup()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"lightdb-latency-{Guid.NewGuid():N}");
        _db = new LightDatabase(new LightOptions
        {
            Path = tempPath,
            AutoCheckpoint = false
        });

        _readKeys = new LightKey[PreloadCount];
        _writeKeys = new LightKey[BatchSize];
        _values = new LightValue[BatchSize];

        var valueTemplate = LightValue.FromString("latency-benchmark-value-数据-测试");

        for (var i = 0; i < PreloadCount; i++)
        {
            _readKeys[i] = LightKey.FromString($"latency:preload:{i:D8}");
            await _db.Put(_readKeys[i], valueTemplate);
        }

        for (var i = 0; i < BatchSize; i++)
        {
            _writeKeys[i] = LightKey.FromString($"latency:write:{i:D8}");
            _values[i] = LightValue.FromString($"latency-value-{i:D8}");
        }

        _batchIndex = 0;
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _batchIndex = (_batchIndex + BatchSize) % PreloadCount;
    }

    /// <summary>
    ///     批量读取延迟
    /// </summary>
    [Benchmark(Description = "BatchGet - 批量读取")]
    public async Task BatchGet()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            await _db.Get<LightValue>(_readKeys[(_batchIndex + i) % PreloadCount]);
        }
    }

    /// <summary>
    ///     批量写入延迟
    /// </summary>
    [Benchmark(Description = "BatchPut - 批量写入")]
    public async Task BatchPut()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            await _db.Put(_writeKeys[i], _values[i]);
        }
    }

    /// <summary>
    ///     混合读写负载（80% 读 20% 写）
    /// </summary>
    [Benchmark(Description = "MixedRW(80R20W) - 混合读写")]
    public async Task MixedReadWrite()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            if (i % 5 == 0)
            {
                await _db.Put(_writeKeys[i % _writeKeys.Length], _values[i % _values.Length]);
            }
            else
            {
                await _db.Get<LightValue>(_readKeys[(_batchIndex + i) % PreloadCount]);
            }
        }
    }
}

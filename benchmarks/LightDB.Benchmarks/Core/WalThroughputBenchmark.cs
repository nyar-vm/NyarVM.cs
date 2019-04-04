using BenchmarkDotNet.Attributes;
using LightDB.Core;
using LightDB.Wal;

namespace LightDB.Benchmarks.Core;

/// <summary>
///     WAL 写吞吐基准测试，验证 ≥100K ops/s 的目标
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class WalThroughputBenchmark
{
    private LightDatabase _db = default!;
    private LightKey _writeKey = default!;
    private LightValue _writeValue = default!;
    private string _tempPath = default!;

    [Params(1000, 10000, 50000)] public int BatchSize { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        _tempPath = Path.Combine(Path.GetTempPath(), $"lightdb-wal-bench-{Guid.NewGuid():N}");
        _db = new LightDatabase(new LightOptions
        {
            Path = _tempPath,
            AutoCheckpoint = false
        });

        _writeKey = LightKey.FromString("wal:bench:key");
        _writeValue = LightValue.FromString(new string('x', 256));

        await Task.CompletedTask;
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();

        try
        {
            if (Directory.Exists(_tempPath))
            {
                Directory.Delete(_tempPath, true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    ///     批量写入吞吐测试
    /// </summary>
    [Benchmark(Description = "WAL 批量写入吞吐 (ops/s)")]
    public async Task WriteBatch()
    {
        for (var i = 0; i < BatchSize; i++)
        {
            await _db.Put(_writeKey, _writeValue);
        }
    }

    /// <summary>
    ///     非事务单条写入延迟（含 WAL Flush）
    /// </summary>
    [Benchmark(Description = "WAL 单条写入延迟 (EveryWrite)")]
    public async Task WriteSingle()
    {
        await _db.Put(_writeKey, _writeValue);
    }
}

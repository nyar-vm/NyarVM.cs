using BenchmarkDotNet.Attributes;
using LightDB.Core;

namespace LightDB.Benchmarks.Core;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class KvOperationBenchmark
{
    private LightDatabase _db;
    private LightKey _readKey;
    private LightKey _writeKey;
    private LightValue _writeValue;

    [Params(1000, 10000, 100000)] public int RecordCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"lightdb-bench-{Guid.NewGuid():N}");
        _db = new LightDatabase(new LightOptions
        {
            Path = tempPath,
            AutoCheckpoint = false
        });

        _readKey = LightKey.FromString("bench:read:key:0");
        _writeKey = LightKey.FromString("bench:write:key");
        _writeValue = LightValue.FromString("benchmark-value-数据");

        for (var i = 0; i < RecordCount; i++)
        {
            var key = LightKey.FromString($"bench:preload:{i:D8}");
            await _db.Put(key, _writeValue);
        }

        await _db.Put(_readKey, _writeValue);
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
    }

    [Benchmark(Description = "PutAsync - 单条写入")]
    public async Task PutAsync_Single()
    {
        await _db.Put(_writeKey, _writeValue);
    }

    [Benchmark(Description = "GetAsync - 单条读取")]
    public async Task GetAsync_Single()
    {
        await _db.Get<LightValue>(_readKey);
    }

    [Benchmark(Description = "DeleteAsync - 单条删除")]
    public async Task DeleteAsync_Single()
    {
        await _db.Delete(_writeKey);
    }

    [Benchmark(Description = "ExistsAsync - 存在判断")]
    public async Task ExistsAsync_Single()
    {
        await _db.Contains(_readKey);
    }
}
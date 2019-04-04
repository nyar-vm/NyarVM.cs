using BenchmarkDotNet.Attributes;
using LightDB.Core;

namespace LightDB.Benchmarks.Editor;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class EditorScenarioBenchmark
{
    private LightDatabase _db;

    [Params(1000, 10000, 100000)] public int RecordCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"lightdb-editor-bench-{Guid.NewGuid():N}");
        _db = new LightDatabase(new LightOptions
        {
            Path = tempPath,
            AutoCheckpoint = false
        });

        for (var i = 0; i < RecordCount; i++)
        {
            var key = LightKey.FromString($"config:/project/main.gg:setting:{i:D8}");
            var value = LightValue.FromString($"value-{i}");
            await _db.Put(key, value);
        }

        for (var i = 0; i < RecordCount; i++)
        {
            var key = LightKey.FromString($"asset:/project/textures/texture_{i:D8}.png");
            var value = LightValue.FromString(
                $"{{\"width\":1024,\"height\":1024,\"format\":\"rgba32\",\"size\":{Random.Shared.Next(1024, 8192)}}}");
            await _db.Put(key, value);
        }

        for (var i = 0; i < 1000; i++)
        {
            var key = LightKey.FromString($"history:/undo/{i:D8}");
            var value = LightValue.FromString(
                $"{{\"op\":\"modify\",\"target\":\"node_{i}\",\"before\":\"old\",\"after\":\"new\"}}");
            await _db.Put(key, value);
        }
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _db.DisposeAsync();
    }

    [Benchmark(Description = "项目配置热加载 - 单条更新")]
    public async Task ConfigHotReload_SingleUpdate()
    {
        var key = LightKey.FromString("config:/project/main.gg:setting:00000000");
        var value = LightValue.FromString($"updated-{DateTimeOffset.UtcNow.Ticks}");
        await _db.Put(key, value);
    }

    [Benchmark(Description = "项目配置热加载 - 批量更新 10 条")]
    public async Task ConfigHotReload_BatchUpdate()
    {
        for (var i = 0; i < 10; i++)
        {
            var key = LightKey.FromString($"config:/project/main.gg:setting:{i:D8}");
            var value = LightValue.FromString($"updated-{i}-{DateTimeOffset.UtcNow.Ticks}");
            await _db.Put(key, value);
        }
    }

    [Benchmark(Description = "资源元数据查询 - 单条查询")]
    public async Task AssetMetadata_SingleQuery()
    {
        var key = LightKey.FromString("asset:/project/textures/texture_00000000.png");
        await _db.Get<LightValue>(key);
    }

    [Benchmark(Description = "资源元数据查询 - 前缀扫描 100 条")]
    public async Task<int> AssetMetadata_PrefixScan()
    {
        var start = LightKey.FromString("asset:/project/textures/texture_00000000");
        var end = LightKey.FromString("asset:/project/textures/texture_00000099");
        var results = await _db.Seek(start).GetRangeAsync(start, end);
        return results.Count;
    }

    [Benchmark(Description = "编辑历史写入 - 1000 步")]
    public async Task EditHistory_Write1000Steps()
    {
        var baseSeq = DateTimeOffset.UtcNow.Ticks;
        for (var i = 0; i < 1000; i++)
        {
            var key = LightKey.FromString($"history:/undo/{baseSeq + i:D20}");
            var value = LightValue.FromString($"{{\"op\":\"modify\",\"step\":{i}}}");
            await _db.Put(key, value);
        }
    }

    [Benchmark(Description = "编辑历史写入 - 事务批量 1000 步")]
    public async Task EditHistory_TransactionBatch1000Steps()
    {
        await using var tx = await _db.BeginTransaction();
        var baseSeq = DateTimeOffset.UtcNow.Ticks;
        for (var i = 0; i < 1000; i++)
        {
            var key = LightKey.FromString($"history:/undo/tx/{baseSeq + i:D20}");
            var value = LightValue.FromString($"{{\"op\":\"modify\",\"step\":{i}}}");
            await tx.Put(key, value);
        }

        await tx.Commit();
    }
}
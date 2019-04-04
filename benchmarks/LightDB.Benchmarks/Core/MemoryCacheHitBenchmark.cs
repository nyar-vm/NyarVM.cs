using BenchmarkDotNet.Attributes;
using LightDB.Cache;
using LightDB.Core;

namespace LightDB.Benchmarks.Core;

/// <summary>
///     内存缓存命中延迟基准测试，验证 <1µs 的目标
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class MemoryCacheHitBenchmark
{
    private LightCache _cache = default!;
    private LightCache _cacheWithTtl = default!;
    private string _hotKey = default!;
    private string _ttlKey = default!;

    [Params(100, 1000, 10000)] public int CacheSize { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"lightdb-cache-hit-bench-{Guid.NewGuid():N}");
        var db = new LightDatabase(new LightOptions
        {
            Path = tempPath,
            AutoCheckpoint = false
        });

        var options = new LightCacheOptions
        {
            MaxEntries = CacheSize,
            Mode = CacheWriteMode.WriteThrough,
            DefaultTtlSeconds = 0
        };

        _cache = new LightCache(db, options);

        var optionsWithTtl = new LightCacheOptions
        {
            MaxEntries = CacheSize,
            Mode = CacheWriteMode.WriteThrough,
            DefaultTtlSeconds = 60
        };

        _cacheWithTtl = new LightCache(db, optionsWithTtl);

        var writeValue = LightValue.FromString("benchmark-value-数据");

        for (var i = 0; i < CacheSize; i++)
        {
            var key = LightKey.FromString($"cache:preload:{i:D8}");
            await db.Put(key, writeValue);
            await _cache.GetAsync<LightValue>(key);
            await _cacheWithTtl.GetAsync<LightValue>(key);
        }

        _hotKey = "cache:preload:00000000";
        _ttlKey = "cache:preload:00000001";
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _cache.DisposeAsync();
        await _cacheWithTtl.DisposeAsync();
    }

    /// <summary>
    ///     热缓存命中延迟（无限 TTL）
    /// </summary>
    [Benchmark(Description = "缓存命中延迟 (无限 TTL)")]
    public async Task CacheHit_NoTtl()
    {
        await _cache.GetAsync<LightValue>(LightKey.FromString(_hotKey));
    }

    /// <summary>
    ///     带 TTL 缓存命中延迟（需检查过期）
    /// </summary>
    [Benchmark(Description = "缓存命中延迟 (60s TTL)")]
    public async Task CacheHit_WithTtl()
    {
        await _cacheWithTtl.GetAsync<LightValue>(LightKey.FromString(_ttlKey));
    }

    /// <summary>
    ///     缓存 LRU 更新延迟（已缓存键重复访问）
    /// </summary>
    [Benchmark(Description = "缓存 LRU 移动延迟")]
    public async Task CacheHit_MoveLru()
    {
        await _cache.GetAsync<LightValue>(LightKey.FromString(_hotKey));
    }

    /// <summary>
    ///     Contains 缓存命中延迟
    /// </summary>
    [Benchmark(Description = "缓存 Contains 命中延迟")]
    public async Task CacheContains_Hit()
    {
        await _cache.ContainsAsync(LightKey.FromString(_hotKey));
    }
}

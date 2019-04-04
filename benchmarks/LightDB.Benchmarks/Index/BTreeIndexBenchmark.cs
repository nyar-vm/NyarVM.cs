using BenchmarkDotNet.Attributes;
using LightDB.Core;
using LightDB.Index;
using LightDB.Storage;

namespace LightDB.Benchmarks.Index;

[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class BTreeIndexBenchmark
{
    private BTreeIndex _index;
    private IPageCache _pageCache;
    private IStorageEngine _storage;

    [Params(1000, 10000, 100000)] public int KeyCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"lightdb-btree-bench-{Guid.NewGuid():N}");
        _storage = new FileStorageEngine(tempPath);
        _pageCache = new PageCache(_storage, 4096);
        _index = new BTreeIndex(_pageCache, "bench");

        for (var i = 0; i < KeyCount; i++)
        {
            var key = LightKey.FromString($"bench:key:{i:D8}");
            var value = LightValue.FromInt64(i);
            _index.InsertAsync(key, value).GetAwaiter().GetResult();
        }
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _pageCache.FlushAsync();
        await _storage.DisposeAsync();
    }

    [Benchmark(Description = "SearchAsync - 点查")]
    public async Task<LightValue?> SearchAsync_Point()
    {
        var mid = KeyCount / 2;
        var key = LightKey.FromString($"bench:key:{mid:D8}");
        return await _index.SearchAsync(key);
    }

    [Benchmark(Description = "InsertAsync - 插入")]
    public async Task InsertAsync_Single()
    {
        var key = LightKey.FromString($"bench:insert:{Guid.NewGuid():N}");
        var value = LightValue.FromString("insert-bench");
        await _index.InsertAsync(key, value);
    }

    [Benchmark(Description = "PrefixScanAsync - 前缀扫描")]
    public async Task<List<LightEntry>> PrefixScanAsync()
    {
        var prefix = LightKey.FromString("bench:key:");
        var results = new List<LightEntry>();
        await foreach (var entry in _index.PrefixScanAsync(prefix))
        {
            results.Add(entry);
            if (results.Count >= 100) break;
        }

        return results;
    }
}
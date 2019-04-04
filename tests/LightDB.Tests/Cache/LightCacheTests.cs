using Core.Database;
using Std.Database.Cache;
using Std.Database.Core;

namespace LightDB.Tests.Cache;

public sealed class LightCacheTests : IDisposable
{
    private readonly LightDatabase _database;
    private readonly string _tempDir;

    public LightCacheTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"lightdb-cache-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _database = new LightDatabase(new LightOptions { path = _tempDir });
    }

    public void Dispose()
    {
        _database.Dispose();

        try
        {
            if (Directory.Exists(_tempDir)) Directory.Delete(_tempDir, true);
        }
        catch
        {
            // 忽略清理失败
        }
    }

    #region TTL 过期测试

    [Fact]
    public async Task Put_WithTtl_EntryExpiresAfterTtl()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0,
            expiration_scan_interval_seconds = 1
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("ttl-key");
        await cache.put(key, "hello", ttlSeconds: 1);

        Assert.True(await cache.ContainsAsync(key));

        await Task.Delay(1500);

        Assert.False(await cache.ContainsAsync(key));
    }

    [Fact]
    public async Task Get_ExpiredEntry_ReturnsDefaultAndCountsMiss()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0,
            expiration_scan_interval_seconds = 1
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("expired-get");
        await cache.put(key, "value", ttlSeconds: 1);

        await Task.Delay(1500);

        var result = await cache.get<string>(key);
        Assert.Null(result);

        var stats = cache.get_statistics();
        Assert.Equal(1, stats.expiration_count);
        Assert.Equal(1, stats.miss_count);
    }

    [Fact]
    public async Task Put_DefaultTtl_UsesOptionsDefault()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 1,
            expiration_scan_interval_seconds = 1
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("default-ttl");
        await cache.put(key, "value");

        Assert.True(await cache.ContainsAsync(key));

        await Task.Delay(1500);

        Assert.False(await cache.ContainsAsync(key));
    }

    [Fact]
    public async Task Put_TtlZero_NeverExpires()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0,
            expiration_scan_interval_seconds = 1
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("no-ttl");
        await cache.put(key, "permanent", ttlSeconds: 0);

        await Task.Delay(1500);

        Assert.True(await cache.ContainsAsync(key));
        var result = await cache.get<string>(key);
        Assert.Equal("permanent", result);
    }

    [Fact]
    public async Task Put_RefreshTtl_ExtendsExpiration()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0,
            expiration_scan_interval_seconds = 1
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("refresh-ttl");
        await cache.put(key, "v1", ttlSeconds: 2);

        await Task.Delay(500);
        await cache.put(key, "v2", ttlSeconds: 2);

        await Task.Delay(1500);

        Assert.True(await cache.ContainsAsync(key));
        var result = await cache.get<string>(key);
        Assert.Equal("v2", result);
    }

    #endregion

    #region LRU 淘汰测试

    [Fact]
    public async Task Put_ExceedsMaxEntries_EvictsLru()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 3,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        await cache.put(DatabaseKey.from_string("key1"), "v1");
        await cache.put(DatabaseKey.from_string("key2"), "v2");
        await cache.put(DatabaseKey.from_string("key3"), "v3");

        Assert.True(await cache.ContainsAsync(DatabaseKey.from_string("key1")));

        await cache.put(DatabaseKey.from_string("key4"), "v4");

        Assert.False(await cache.ContainsAsync(DatabaseKey.from_string("key1")));

        var stats = cache.get_statistics();
        Assert.Equal(1, stats.eviction_count);
    }

    [Fact]
    public async Task Get_MovesToHead_PreventsEviction()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 3,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        await cache.put(DatabaseKey.from_string("key1"), "v1");
        await cache.put(DatabaseKey.from_string("key2"), "v2");
        await cache.put(DatabaseKey.from_string("key3"), "v3");

        _ = await cache.get<string>(DatabaseKey.from_string("key1"));

        await cache.put(DatabaseKey.from_string("key4"), "v4");

        Assert.True(await cache.ContainsAsync(DatabaseKey.from_string("key1")));
        Assert.False(await cache.ContainsAsync(DatabaseKey.from_string("key2")));
    }

    [Fact]
    public async Task Put_UpdateExisting_DoesNotEvict()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 3,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        await cache.put(DatabaseKey.from_string("key1"), "v1");
        await cache.put(DatabaseKey.from_string("key2"), "v2");
        await cache.put(DatabaseKey.from_string("key3"), "v3");

        await cache.put(DatabaseKey.from_string("key1"), "v1-updated");

        var stats = cache.get_statistics();
        Assert.Equal(0, stats.eviction_count);
        Assert.Equal(3, stats.entry_count);

        var result = await cache.get<string>(DatabaseKey.from_string("key1"));
        Assert.Equal("v1-updated", result);
    }

    [Fact]
    public async Task Eviction_Statistics_Tracked()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 2,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        await cache.put(DatabaseKey.from_string("a"), "1");
        await cache.put(DatabaseKey.from_string("b"), "2");
        await cache.put(DatabaseKey.from_string("c"), "3");
        await cache.put(DatabaseKey.from_string("d"), "4");

        var stats = cache.get_statistics();
        Assert.Equal(2, stats.eviction_count);
        Assert.Equal(2, stats.entry_count);
    }

    #endregion

    #region Write-Through 模式测试

    [Fact]
    public async Task WriteThrough_Put_WritesToDatabaseImmediately()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            mode = CacheMode.WriteThrough
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("wt-key");
        await cache.put(key, "wt-value");

        var dbValue = await _database.Get<string>(key);
        Assert.Equal("wt-value", dbValue);
    }

    [Fact]
    public async Task WriteThrough_Remove_DoesNotAffectDatabase()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            mode = CacheMode.WriteThrough
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("wt-remove");
        await cache.put(key, "value");

        await cache.RemoveAsync(key);

        Assert.False(await cache.ContainsAsync(key));

        var dbValue = await _database.Get<string>(key);
        Assert.Equal("value", dbValue);
    }

    [Fact]
    public async Task WriteThrough_MultipleWrites_AllPersisted()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            mode = CacheMode.WriteThrough
        };
        await using var cache = new DatabaseCache(_database, options);

        for (var i = 0; i < 10; i++) await cache.put(DatabaseKey.from_string($"wt-multi-{i}"), $"value-{i}");

        for (var i = 0; i < 10; i++)
        {
            var dbValue = await _database.Get<string>(DatabaseKey.from_string($"wt-multi-{i}"));
            Assert.Equal($"value-{i}", dbValue);
        }
    }

    #endregion

    #region Write-Back 模式测试

    [Fact]
    public async Task WriteBack_Put_QueuesForLaterFlush()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            mode = CacheMode.WriteBack,
            write_back_flush_interval_ms = 500,
            write_back_batch_size = 100
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("wb-key");
        await cache.put(key, "wb-value");

        var cachedValue = await cache.get<string>(key);
        Assert.Equal("wb-value", cachedValue);

        await Task.Delay(1000);

        var dbValue = await _database.Get<string>(key);
        Assert.Equal("wb-value", dbValue);
    }

    [Fact]
    public async Task WriteBack_Dispose_FlushesRemaining()
    {
        var key = DatabaseKey.from_string("wb-dispose");

        {
            var options = new LightCacheOptions
            {
                MaxEntries = 100,
                mode = CacheMode.WriteBack,
                write_back_flush_interval_ms = 60000,
                write_back_batch_size = 100
            };
            var cache = new DatabaseCache(_database, options);
            await cache.put(key, "wb-dispose-value");
            await cache.DisposeAsync();
        }

        var dbValue = await _database.Get<string>(key);
        Assert.Equal("wb-dispose-value", dbValue);
    }

    [Fact]
    public async Task WriteBack_BatchFull_FlushesImmediately()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            mode = CacheMode.WriteBack,
            write_back_flush_interval_ms = 60000,
            write_back_batch_size = 3
        };
        await using var cache = new DatabaseCache(_database, options);

        for (var i = 0; i < 3; i++) await cache.put(DatabaseKey.from_string($"wb-batch-{i}"), $"value-{i}");

        for (var i = 0; i < 3; i++)
        {
            var dbValue = await _database.Get<string>(DatabaseKey.from_string($"wb-batch-{i}"));
            Assert.Equal($"value-{i}", dbValue);
        }
    }

    #endregion

    #region 缓存命中/未命中统计测试

    [Fact]
    public async Task Statistics_HitAndMiss_Counted()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        var hitKey = DatabaseKey.from_string("hit-key");
        await cache.put(hitKey, "hit-value");

        _ = await cache.get<string>(hitKey);
        _ = await cache.get<string>(DatabaseKey.from_string("miss-key"));

        var stats = cache.get_statistics();
        Assert.Equal(1, stats.hit_count);
        Assert.Equal(1, stats.miss_count);
    }

    [Fact]
    public async Task Statistics_HitRate_Calculated()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        await cache.put(DatabaseKey.from_string("k1"), "v1");

        _ = await cache.get<string>(DatabaseKey.from_string("k1"));
        _ = await cache.get<string>(DatabaseKey.from_string("k1"));
        _ = await cache.get<string>(DatabaseKey.from_string("miss"));

        var stats = cache.get_statistics();
        Assert.Equal(2, stats.hit_count);
        Assert.Equal(1, stats.miss_count);
        Assert.Equal(2.0 / 3.0, stats.hit_rate, 3);
    }

    #endregion

    #region 缓存未命中回源测试

    [Fact]
    public async Task Get_CacheMiss_LoadsFromDatabase()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("backfill-key");
        await _database.put(key, "db-value");

        var result = await cache.get<string>(key);
        Assert.Equal("db-value", result);

        var stats = cache.get_statistics();
        Assert.Equal(1, stats.miss_count);
        Assert.Equal(1, stats.entry_count);
    }

    [Fact]
    public async Task Get_CacheMiss_BackfillCached()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("backfill-cache");
        await _database.put(key, "db-value");

        _ = await cache.get<string>(key);

        var stats1 = cache.get_statistics();
        Assert.Equal(1, stats1.MissCount);

        _ = await cache.get<string>(key);

        var stats2 = cache.get_statistics();
        Assert.Equal(1, stats2.HitCount);
    }

    #endregion

    #region 删除测试

    [Fact]
    public async Task Remove_ExistingKey_RemovedFromCache()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("remove-key");
        await cache.put(key, "value");

        Assert.True(await cache.ContainsAsync(key));

        await cache.RemoveAsync(key);

        Assert.False(await cache.ContainsAsync(key));
    }

    [Fact]
    public async Task Remove_NonExistentKey_NoException()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        await cache.RemoveAsync(DatabaseKey.from_string("nonexistent"));
    }

    #endregion

    #region Contains 测试

    [Fact]
    public async Task Contains_ExpiredEntry_ReturnsFalse()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0,
            expiration_scan_interval_seconds = 1
        };
        await using var cache = new DatabaseCache(_database, options);

        var key = DatabaseKey.from_string("contains-expired");
        await cache.put(key, "value", ttlSeconds: 1);

        Assert.True(await cache.ContainsAsync(key));

        await Task.Delay(1500);

        Assert.False(await cache.ContainsAsync(key));
    }

    [Fact]
    public async Task Contains_NonExistentKey_ReturnsFalse()
    {
        var options = new LightCacheOptions
        {
            MaxEntries = 100,
            default_ttl_seconds = 0
        };
        await using var cache = new DatabaseCache(_database, options);

        Assert.False(await cache.ContainsAsync(DatabaseKey.from_string("no-such-key")));
    }

    #endregion
}
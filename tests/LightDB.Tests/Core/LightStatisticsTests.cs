using Std.Database.Core;

namespace LightDB.Tests.Core;

public sealed class DatabaseStatisticsTests
{
    [Fact]
    public void DefaultValues_ShouldBeZero()
    {
        var stats = new DatabaseStatistics();

        Assert.Equal(0, stats.TotalEntries);
        Assert.Equal(0, stats.read_count);
        Assert.Equal(0, stats.write_count);
        Assert.Equal(0, stats.delete_count);
        Assert.Equal(0, stats.CacheHits);
        Assert.Equal(0, stats.CacheMisses);
        Assert.Equal(0, stats.ActiveTransactions);
        Assert.Equal(0, stats.DatabaseSize);
        Assert.Equal(0, stats.WalSize);
    }

    [Fact]
    public void CacheHitRate_WithNoAccess_ShouldBeZero()
    {
        var stats = new DatabaseStatistics();

        Assert.Equal(0, stats.CacheHitRate);
    }

    [Fact]
    public void CacheHitRate_WithHitsAndMisses_ShouldCalculateCorrectly()
    {
        var stats = new DatabaseStatistics
        {
            CacheHits = 7,
            CacheMisses = 3
        };

        Assert.Equal(0.7, stats.CacheHitRate, 2);
    }

    [Fact]
    public void CacheHitRate_WithAllHits_ShouldBeOne()
    {
        var stats = new DatabaseStatistics
        {
            CacheHits = 10,
            CacheMisses = 0
        };

        Assert.Equal(1.0, stats.CacheHitRate);
    }
}
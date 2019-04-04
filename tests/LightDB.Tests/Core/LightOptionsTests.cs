using Std.Database.Core;
using Std.Database.Wal;

namespace LightDB.Tests.Core;

public sealed class LightOptionsTests
{
    [Fact]
    public void Default_ShouldHaveExpectedValues()
    {
        var options = LightOptions.@default;

        Assert.Equal(".light/db", options.path);
        Assert.Equal(128, options.b_tree_order);
        Assert.Equal(4096, options.PageSize);
        Assert.False(options.read_only);
        Assert.True(options.auto_checkpoint);
        Assert.Equal(30000, options.checkpoint_interval_ms);
        Assert.Equal(WalFlushPolicy.batch, options.wal_flush_policy);
        Assert.Equal(1024, options.CacheSize);
        Assert.False(options.enable_compression);
    }

    [Fact]
    public void CustomOptions_ShouldAllowOverrides()
    {
        var options = new LightOptions
        {
            path = "/tmp/testdb",
            b_tree_order = 64,
            PageSize = 8192,
            read_only = true,
            wal_flush_policy = WalFlushPolicy.every_write
        };

        Assert.Equal("/tmp/testdb", options.path);
        Assert.Equal(64, options.b_tree_order);
        Assert.Equal(8192, options.PageSize);
        Assert.True(options.read_only);
        Assert.Equal(WalFlushPolicy.every_write, options.wal_flush_policy);
    }
}
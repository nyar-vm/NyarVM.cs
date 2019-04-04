using Std.Database.Core;

namespace LightDB.Tests.Core;

public sealed class VersionStoreTests
{
    [Fact]
    public void GetVisibleValue_WithNoVersions_ShouldReturnNull()
    {
        var store = new VersionStore();
        var key = DatabaseKey.from_string("key1");

        var result = store.GetVisibleValue(key, new SequenceNumber(1));

        Assert.Null(result);
    }

    [Fact]
    public void AddVersion_ThenGetVisibleValue_ShouldReturnValue()
    {
        var store = new VersionStore();
        var key = DatabaseKey.from_string("key1");
        var value = DatabaseValue.from_string("value1");

        store.AddVersion(key, new SequenceNumber(1), value);

        var result = store.GetVisibleValue(key, new SequenceNumber(1));

        Assert.NotNull(result);
        Assert.Equal("value1", result.Value.ToString());
    }

    [Fact]
    public void GetVisibleValue_WithMultipleVersions_ShouldReturnLatestVisible()
    {
        var store = new VersionStore();
        var key = DatabaseKey.from_string("key1");

        store.AddVersion(key, new SequenceNumber(1), DatabaseValue.from_string("v1"));
        store.AddVersion(key, new SequenceNumber(3), DatabaseValue.from_string("v3"));
        store.AddVersion(key, new SequenceNumber(5), DatabaseValue.from_string("v5"));

        var atSeq2 = store.GetVisibleValue(key, new SequenceNumber(2));
        var atSeq3 = store.GetVisibleValue(key, new SequenceNumber(3));
        var atSeq4 = store.GetVisibleValue(key, new SequenceNumber(4));
        var atSeq5 = store.GetVisibleValue(key, new SequenceNumber(5));
        var atSeq10 = store.GetVisibleValue(key, new SequenceNumber(10));

        Assert.NotNull(atSeq2);
        Assert.Equal("v1", atSeq2.Value.ToString());

        Assert.NotNull(atSeq3);
        Assert.Equal("v3", atSeq3.Value.ToString());

        Assert.NotNull(atSeq4);
        Assert.Equal("v3", atSeq4.Value.ToString());

        Assert.NotNull(atSeq5);
        Assert.Equal("v5", atSeq5.Value.ToString());

        Assert.NotNull(atSeq10);
        Assert.Equal("v5", atSeq10.Value.ToString());
    }

    [Fact]
    public void GetVisibleValue_WithSequenceBeforeAllVersions_ShouldReturnNull()
    {
        var store = new VersionStore();
        var key = DatabaseKey.from_string("key1");

        store.AddVersion(key, new SequenceNumber(5), DatabaseValue.from_string("v5"));

        var result = store.GetVisibleValue(key, new SequenceNumber(3));

        Assert.Null(result);
    }

    [Fact]
    public void GetVisibleValue_WithDifferentKeys_ShouldIsolateVersions()
    {
        var store = new VersionStore();
        var key1 = DatabaseKey.from_string("key1");
        var key2 = DatabaseKey.from_string("key2");

        store.AddVersion(key1, new SequenceNumber(1), DatabaseValue.from_string("val1"));
        store.AddVersion(key2, new SequenceNumber(2), DatabaseValue.from_string("val2"));

        var result1 = store.GetVisibleValue(key1, new SequenceNumber(10));
        var result2 = store.GetVisibleValue(key2, new SequenceNumber(10));

        Assert.NotNull(result1);
        Assert.Equal("val1", result1.Value.ToString());

        Assert.NotNull(result2);
        Assert.Equal("val2", result2.Value.ToString());
    }

    [Fact]
    public void Cleanup_ShouldRemoveOldVersions()
    {
        var store = new VersionStore();
        var key = DatabaseKey.from_string("key1");

        store.AddVersion(key, new SequenceNumber(1), DatabaseValue.from_string("v1"));
        store.AddVersion(key, new SequenceNumber(3), DatabaseValue.from_string("v3"));
        store.AddVersion(key, new SequenceNumber(5), DatabaseValue.from_string("v5"));

        store.Cleanup(new SequenceNumber(4));

        var atSeq3 = store.GetVisibleValue(key, new SequenceNumber(3));
        Assert.Null(atSeq3);

        var atSeq5 = store.GetVisibleValue(key, new SequenceNumber(5));
        Assert.NotNull(atSeq5);
        Assert.Equal("v5", atSeq5.Value.ToString());
    }

    [Fact]
    public void Cleanup_ShouldRemoveKeyWithNoRemainingVersions()
    {
        var store = new VersionStore();
        var key = DatabaseKey.from_string("key1");

        store.AddVersion(key, new SequenceNumber(1), DatabaseValue.from_string("v1"));
        store.AddVersion(key, new SequenceNumber(2), DatabaseValue.from_string("v2"));

        store.Cleanup(new SequenceNumber(10));

        var result = store.GetVisibleValue(key, new SequenceNumber(10));
        Assert.Null(result);
    }

    [Fact]
    public void Cleanup_WithMinActiveSequenceZero_ShouldRemoveNothing()
    {
        var store = new VersionStore();
        var key = DatabaseKey.from_string("key1");

        store.AddVersion(key, new SequenceNumber(1), DatabaseValue.from_string("v1"));

        store.Cleanup(new SequenceNumber(0));

        var result = store.GetVisibleValue(key, new SequenceNumber(1));
        Assert.NotNull(result);
        Assert.Equal("v1", result.Value.ToString());
    }
}
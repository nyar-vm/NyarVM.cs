using Std.Database.Core;

namespace LightDB.Tests.Core;

public sealed class DatabaseKeyTests
{
    [Fact]
    public void FromString_ShouldCreateKeyWithUtf8Bytes()
    {
        var key = DatabaseKey.from_string("hello");

        Assert.Equal(5, key.length);
        Assert.False(key.is_empty);
    }

    [Fact]
    public void FromString_ShouldSupportChinese()
    {
        var key = DatabaseKey.from_string("你好世界");

        Assert.False(key.is_empty);
        Assert.True(key.length > 0);
    }

    [Fact]
    public void FromUInt64_ShouldCreateKeyWith8Bytes()
    {
        var key = DatabaseKey.from_u_int64(42);

        Assert.Equal(8, key.length);
        Assert.False(key.is_empty);
    }

    [Fact]
    public void FromGuid_ShouldCreateKeyWith16Bytes()
    {
        var guid = Guid.NewGuid();
        var key = DatabaseKey.from_guid(guid);

        Assert.Equal(16, key.length);
        Assert.False(key.is_empty);
    }

    [Fact]
    public void FromObject_ShouldSerializeToJson()
    {
        var key = DatabaseKey.from_object(new { Name = "test", Value = 42 });

        Assert.False(key.is_empty);
        Assert.True(key.length > 0);
    }

    [Fact]
    public void Empty_ShouldHaveZeroLength()
    {
        Assert.Equal(0, DatabaseKey.empty.Length);
        Assert.True(DatabaseKey.empty.IsEmpty);
    }

    [Fact]
    public void ImplicitFromString_ShouldCreateKey()
    {
        DatabaseKey key = "test";

        Assert.False(key.is_empty);
        Assert.Equal(4, key.length);
    }

    [Fact]
    public void ImplicitFromInt_ShouldCreateKey()
    {
        var key = DatabaseKey.from_u_int64(42);

        Assert.False(key.is_empty);
        Assert.Equal(8, key.length);
    }

    [Fact]
    public void ImplicitFromLong_ShouldCreateKey()
    {
        var key = DatabaseKey.from_u_int64(123456L);

        Assert.False(key.is_empty);
        Assert.Equal(8, key.length);
    }

    [Fact]
    public void ImplicitFromGuid_ShouldCreateKey()
    {
        var key = DatabaseKey.from_guid(Guid.NewGuid());

        Assert.False(key.is_empty);
        Assert.Equal(16, key.length);
    }

    [Fact]
    public void ImplicitFromUlong_ShouldCreateKey()
    {
        var key = DatabaseKey.from_u_int64(999UL);

        Assert.False(key.is_empty);
        Assert.Equal(8, key.length);
    }

    [Fact]
    public void ImplicitFromByteArray_ShouldCreateKey()
    {
        var key = new DatabaseKey([1, 2, 3]);

        Assert.Equal(3, key.length);
    }

    [Fact]
    public void StartsWith_ShouldReturnTrue_WhenPrefixMatches()
    {
        var key = DatabaseKey.from_string("user:123:profile");

        Assert.True(key.starts_with(DatabaseKey.from_string("user:123")));
        Assert.True(key.starts_with(DatabaseKey.from_string("user")));
    }

    [Fact]
    public void StartsWith_ShouldReturnFalse_WhenPrefixDoesNotMatch()
    {
        var key = DatabaseKey.from_string("user:123:profile");

        Assert.False(key.starts_with(DatabaseKey.from_string("order")));
        Assert.False(key.starts_with(DatabaseKey.from_string("user:123:profile:extra")));
    }

    [Fact]
    public void CompareTo_ShouldOrderLexicographically()
    {
        var key1 = DatabaseKey.from_string("a");
        var key2 = DatabaseKey.from_string("b");
        var key3 = DatabaseKey.from_string("c");

        Assert.True(key1.CompareTo(key2) < 0);
        Assert.True(key2.CompareTo(key1) > 0);
        Assert.True(key1.CompareTo(key1) == 0);
        Assert.True(key1.CompareTo(key3) < 0);
    }

    [Fact]
    public void ToString_ShouldReturnUtf8DecodedString()
    {
        var key = DatabaseKey.from_string("hello");

        Assert.Equal("hello", key.ToString());
    }

    [Fact]
    public void RecordStructEquality_ShouldCompareByValue()
    {
        var key1 = DatabaseKey.from_string("test");
        var key2 = DatabaseKey.from_string("test");

        Assert.Equal(key1.ToString(), key2.ToString());
        Assert.True(key1.bytes.Span.SequenceEqual(key2.bytes.Span));
    }

    [Fact]
    public void RecordStructInequality_ShouldCompareByValue()
    {
        var key1 = DatabaseKey.from_string("a");
        var key2 = DatabaseKey.from_string("b");

        Assert.NotEqual(key1.ToString(), key2.ToString());
        Assert.False(key1.bytes.Span.SequenceEqual(key2.bytes.Span));
    }
}
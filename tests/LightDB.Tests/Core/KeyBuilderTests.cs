using Std.Database.Core;

namespace LightDB.Tests.Core;

public sealed class DatabaseKeyBuilderTests
{
    [Fact]
    public void From_WithPrefix_ShouldCreateKey()
    {
        var key = DatabaseKeyBuilder.From("user").Build();

        Assert.Equal("user", key.ToString());
    }

    [Fact]
    public void Append_String_ShouldAddColonSeparatedSegment()
    {
        var key = DatabaseKeyBuilder.From("user").Append("123").Build();

        Assert.Equal("user:123", key.ToString());
    }

    [Fact]
    public void Append_Int_ShouldAddColonSeparatedSegment()
    {
        var key = DatabaseKeyBuilder.From("item").Append(42).Build();

        Assert.Equal("item:42", key.ToString());
    }

    [Fact]
    public void Append_Long_ShouldAddColonSeparatedSegment()
    {
        var key = DatabaseKeyBuilder.From("item").Append(123456L).Build();

        Assert.Equal("item:123456", key.ToString());
    }

    [Fact]
    public void Append_Guid_ShouldAddColonSeparatedSegment()
    {
        var guid = new Guid("01020304-0506-0708-090a-0b0c0d0e0f10");
        var key = DatabaseKeyBuilder.From("entity").Append(guid).Build();

        Assert.Equal($"entity:{guid}", key.ToString());
    }

    [Fact]
    public void AppendFormat_ShouldAddFormattedSegment()
    {
        var key = DatabaseKeyBuilder.From("log").AppendFormat("{0:D4}", 42).Build();

        Assert.Equal("log:0042", key.ToString());
    }

    [Fact]
    public void MultipleAppends_ShouldChainCorrectly()
    {
        var key = DatabaseKeyBuilder.From("user")
            .Append("123")
            .Append("profile")
            .Build();

        Assert.Equal("user:123:profile", key.ToString());
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnDatabaseKey()
    {
        DatabaseKey key = DatabaseKeyBuilder.From("user").Append("456");

        Assert.Equal("user:456", key.ToString());
    }

    [Fact]
    public void Build_ShouldReturnDatabaseKey()
    {
        var key = DatabaseKeyBuilder.From("config").Append("theme").Build();

        Assert.IsType<DatabaseKey>(key);
        Assert.Equal("config:theme", key.ToString());
    }
}
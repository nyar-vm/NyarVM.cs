using Oak.Diagnostics;

namespace Oak.Json.Tests;

public class JsonParserTests : JsonTestBase
{
    [Fact]
    public void ParseNull()
    {
        var (value, diagnostics) = ParseWithTimeout("null");
        Assert.False(diagnostics.HasErrors);
        Assert.NotNull(value);
        Assert.Equal(JsonValueType.Null, value.ValueType);
    }

    [Fact]
    public void ParseTrue()
    {
        var (value, diagnostics) = ParseWithTimeout("true");
        Assert.False(diagnostics.HasErrors);
        var boolean = Assert.IsType<JsonBoolean>(value);
        Assert.True(boolean.Value);
    }

    [Fact]
    public void ParseInteger()
    {
        var (value, diagnostics) = ParseWithTimeout("42");
        Assert.False(diagnostics.HasErrors);
        var number = Assert.IsType<JsonNumber>(value);
        Assert.Equal(42.0, number.Value);
    }

    [Fact]
    public void ParseString()
    {
        var (value, diagnostics) = ParseWithTimeout("\"hello world\"");
        Assert.False(diagnostics.HasErrors);
        var str = Assert.IsType<JsonString>(value);
        Assert.Equal("hello world", str.Value);
    }

    [Fact]
    public void ParseEmptyArray()
    {
        var (value, diagnostics) = ParseWithTimeout("[]");
        Assert.False(diagnostics.HasErrors);
        var array = Assert.IsType<JsonArray>(value);
        Assert.Empty(array.Items);
    }

    [Fact]
    public void ParseArrayWithElements()
    {
        var (value, diagnostics) = ParseWithTimeout("[1, \"two\", true]");
        Assert.False(diagnostics.HasErrors);
        var array = Assert.IsType<JsonArray>(value);
        Assert.Equal(3, array.Count);
    }

    [Fact]
    public void ParseEmptyObject()
    {
        var (value, diagnostics) = ParseWithTimeout("{}");
        Assert.False(diagnostics.HasErrors);
        var obj = Assert.IsType<JsonObject>(value);
        Assert.Equal(0, obj.Count);
    }

    [Fact]
    public void ParseObjectWithProperties()
    {
        var (value, diagnostics) = ParseWithTimeout("{\"name\": \"Alice\", \"age\": 30}");
        Assert.False(diagnostics.HasErrors);
        var obj = Assert.IsType<JsonObject>(value);
        Assert.Equal(2, obj.Count);
        Assert.Equal("Alice", Assert.IsType<JsonString>(obj["name"]!).Value);
        Assert.Equal(30.0, Assert.IsType<JsonNumber>(obj["age"]!).Value);
    }

    [Fact]
    public void ParseNestedObject()
    {
        var (value, diagnostics) = ParseWithTimeout("{\"person\": {\"name\": \"Bob\"}}");
        Assert.False(diagnostics.HasErrors);
        var obj = Assert.IsType<JsonObject>(value);
        var inner = Assert.IsType<JsonObject>(obj["person"]!);
        Assert.Equal("Bob", Assert.IsType<JsonString>(inner["name"]!).Value);
    }

    [Fact]
    public void ParseMissingClosingBrace_ProducesError()
    {
        var (_, diagnostics) = ParseWithTimeout("{\"a\": 1");
        Assert.True(diagnostics.HasErrors);
    }
}

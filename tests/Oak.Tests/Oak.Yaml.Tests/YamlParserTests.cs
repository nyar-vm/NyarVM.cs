using Oak.Diagnostics;

namespace Oak.Yaml.Tests;

public class YamlParserTests : YamlTestBase
{
    [Fact]
    public void ParseNull()
    {
        var (value, diagnostics) = ParseWithTimeout("key: null");
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        Assert.True(mapping.ContainsKey("key"));
        Assert.IsType<YamlNull>(mapping["key"]!);
    }

    [Fact]
    public void ParseBooleanTrue()
    {
        var (value, diagnostics) = ParseWithTimeout("key: true");
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        var boolean = Assert.IsType<YamlBoolean>(mapping["key"]!);
        Assert.True(boolean.Value);
    }

    [Fact]
    public void ParseNumber()
    {
        var (value, diagnostics) = ParseWithTimeout("key: 42");
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        var number = Assert.IsType<YamlNumber>(mapping["key"]!);
        Assert.Equal(42.0, number.Value);
    }

    [Fact]
    public void ParseString()
    {
        var (value, diagnostics) = ParseWithTimeout("key: hello");
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        var str = Assert.IsType<YamlString>(mapping["key"]!);
        Assert.Equal("hello", str.Value);
    }

    [Fact]
    public void ParseNestedMapping()
    {
        var source = """
            server:
              host: localhost
              port: 8080
            """;
        var (value, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        var root = Assert.IsType<YamlMapping>(value);
        var server = Assert.IsType<YamlMapping>(root["server"]!);
        Assert.Equal("localhost", Assert.IsType<YamlString>(server["host"]!).Value);
    }

    [Fact]
    public void ParseSequence()
    {
        var source = """
            - apple
            - banana
            - cherry
            """;
        var (value, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        var seq = Assert.IsType<YamlSequence>(value);
        Assert.Equal(3, seq.Count);
    }

    [Fact]
    public void ParseFlowMapping()
    {
        var (value, diagnostics) = ParseWithTimeout("{a: 1, b: 2}");
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        Assert.Equal(2, mapping.Count);
    }
}

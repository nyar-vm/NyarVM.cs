namespace Oak.Yaml.Tests;

public class YamlParserRegressionTests : YamlTestBase
{
    [Fact]
    public void ParseBooleanFalse()
    {
        var (value, diagnostics) = ParseWithTimeout("key: false");
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        var boolean = Assert.IsType<YamlBoolean>(mapping["key"]!);
        Assert.False(boolean.Value);
    }

    [Fact]
    public void ParseFloatNumber()
    {
        var (value, diagnostics) = ParseWithTimeout("key: 3.14");
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        var number = Assert.IsType<YamlNumber>(mapping["key"]!);
        Assert.Equal(3.14, number.Value);
    }

    [Fact]
    public void ParseQuotedString()
    {
        var (value, diagnostics) = ParseWithTimeout("key: \"hello world\"");
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        var str = Assert.IsType<YamlString>(mapping["key"]!);
        Assert.Equal("hello world", str.Value);
    }

    [Fact]
    public void ParseFlowMapping()
    {
        var (value, diagnostics) = ParseWithTimeout("{a: 1, b: 2}");
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        Assert.Equal(2, mapping.Count);
    }

    [Fact]
    public void ParseSequenceWithMapping()
    {
        var source = "- apple\n- banana";
        var (value, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        var seq = Assert.IsType<YamlSequence>(value);
        Assert.Equal(2, seq.Count);
    }

    [Fact]
    public void ParseMultipleKeys()
    {
        var source = """
            name: Alice
            age: 30
            active: true
            """;
        var (value, diagnostics) = ParseWithTimeout(source);
        Assert.False(diagnostics.HasErrors);
        var mapping = Assert.IsType<YamlMapping>(value);
        Assert.Equal(3, mapping.Count);
    }
}

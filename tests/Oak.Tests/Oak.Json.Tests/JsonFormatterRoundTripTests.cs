using Oak.Json;

namespace Oak.Json.Tests;

public class JsonFormatterRoundTripTests : JsonTestBase
{
    private void AssertRoundTrip(string source)
    {
        var (value, _) = ParseWithTimeout(source);
        var formatted = JsonFormatter.Format(value, indent: false);
        var (reparsed, _) = ParseWithTimeout(formatted);

        var reformatted = JsonFormatter.Format(reparsed, indent: false);
        Assert.Equal(formatted, reformatted);
    }

    [Fact]
    public void Format_Null_ShouldRoundTrip()
    {
        AssertRoundTrip("null");
    }

    [Fact]
    public void Format_True_ShouldRoundTrip()
    {
        AssertRoundTrip("true");
    }

    [Fact]
    public void Format_False_ShouldRoundTrip()
    {
        AssertRoundTrip("false");
    }

    [Fact]
    public void Format_Integer_ShouldRoundTrip()
    {
        AssertRoundTrip("42");
    }

    [Fact]
    public void Format_String_ShouldRoundTrip()
    {
        AssertRoundTrip("\"hello\"");
    }

    [Fact]
    public void Format_EmptyArray_ShouldRoundTrip()
    {
        AssertRoundTrip("[]");
    }

    [Fact]
    public void Format_SimpleArray_ShouldRoundTrip()
    {
        AssertRoundTrip("[1,2,3]");
    }

    [Fact]
    public void Format_EmptyObject_ShouldRoundTrip()
    {
        AssertRoundTrip("{}");
    }

    [Fact]
    public void Format_SimpleObject_ShouldRoundTrip()
    {
        AssertRoundTrip("{\"key\":\"value\"}");
    }

    [Fact]
    public void Format_NestedObject_ShouldRoundTrip()
    {
        AssertRoundTrip("{\"user\":{\"name\":\"John\",\"age\":30}}");
    }

    [Fact]
    public void Format_ComplexNested_ShouldRoundTrip()
    {
        AssertRoundTrip("{\"users\":[{\"id\":1},{\"id\":2}],\"total\":2}");
    }
}

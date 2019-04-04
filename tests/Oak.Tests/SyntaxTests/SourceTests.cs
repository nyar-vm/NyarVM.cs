using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;


/// <summary>

/// ISource 和 StringSource 测试


/// </summary>
public class SourceTests
{
    [Fact]
    public void StringSource_Indexer()
    {
        var source = new StringSource("hello");
        Assert.Equal('h', source[0]);
        Assert.Equal('o', source[4]);
    }

    [Fact]
    public void StringSource_Length()
    {
        var source = new StringSource("hello");
        Assert.Equal(5, source.length);
    }

    [Fact]
    public void StringSource_Substring()
    {
        var source = new StringSource("hello world");
        Assert.Equal("lo wo", source.substring(new Range(3, 8)));
    }
}

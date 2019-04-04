using Oak.Syntax;
using Oak.Utilities;

namespace Oak.Core.Tests.SyntaxTests;


/// <summary>

/// LineIndex 行索引工具测试


/// </summary>
public class LineIndexTests
{
    [Fact]
    public void LineIndex_GetLineColumn()
    {
        var source = new StringSource("hello\nworld\n");
        var index = new LineIndex(source);
        var (line, col) = index.get_line_column(0);
        Assert.Equal(1, line);
        Assert.Equal(1, col);

        var (line2, col2) = index.get_line_column(6);
        Assert.Equal(2, line2);
        Assert.Equal(1, col2);
    }

    [Fact]
    public void LineIndex_GetOffset()
    {
        var source = new StringSource("hello\nworld\n");
        var index = new LineIndex(source);
        Assert.Equal(0, index.get_offset(1, 1));
        Assert.Equal(6, index.get_offset(2, 1));
    }

    [Fact]
    public void LineIndex_LineCount()
    {
        var source = new StringSource("hello\nworld\n");
        var index = new LineIndex(source);
        Assert.Equal(3, index.line_count);
    }

    [Fact]
    public void LineIndex_SingleLine()
    {
        var source = new StringSource("hello");
        var index = new LineIndex(source);
        Assert.Equal(1, index.line_count);
        var (line, col) = index.get_line_column(3);
        Assert.Equal(1, line);
        Assert.Equal(4, col);
    }
}

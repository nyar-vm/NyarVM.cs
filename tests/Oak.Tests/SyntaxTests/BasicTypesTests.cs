using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;


/// <summary>

/// TextSpan、Edit、NodeKind 基础类型测试


/// </summary>
public class BasicTypesTests
{
    [Fact]
    public void TextSpan_Properties()
    {
        var span = default(TextSpan);
        Assert.Equal(10, span.start);
        Assert.Equal(5, span.length);
        Assert.Equal(15, span.end);
    }

    [Fact]
    public void TextSpan_Contains()
    {
        var span = default(TextSpan);
        Assert.True(span.contains(10));
        Assert.True(span.contains(12));
        Assert.True(span.contains(14));
        Assert.False(span.contains(9));
        Assert.False(span.contains(15));
    }

    [Fact]
    public void TextSpan_OverlapsWith()
    {
        var span1 = default(TextSpan);
        var span2 = default(TextSpan);
        var span3 = default(TextSpan);
        Assert.True(span1.overlaps_with(span2));
        Assert.False(span1.overlaps_with(span3));
    }

    [Fact]
    public void TextSpan_Equality()
    {
        var span1 = default(TextSpan);
        var span2 = default(TextSpan);
        var span3 = default(TextSpan);
        Assert.Equal(span1, span2);
        Assert.NotEqual(span1, span3);
    }

    [Fact]
    public void Edit_Properties()
    {
        var edit = new Edit(default(TextSpan), "abc");
        Assert.Equal(default(TextSpan), edit.old_span);
        Assert.Equal("abc", edit.new_text);
        Assert.Equal(0, edit.delta);
    }

    [Fact]
    public void Edit_Delta_Positive()
    {
        var edit = new Edit(default(TextSpan), "abcd");
        Assert.Equal(2, edit.delta);
    }

    [Fact]
    public void Edit_Delta_Negative()
    {
        var edit = new Edit(default(TextSpan), "ab");
        Assert.Equal(-3, edit.delta);
    }

    [Fact]
    public void NodeKind_ImplicitConversion()
    {
        NodeKind kind = 42;
        Assert.Equal(42, kind.value);
        int value = kind;
        Assert.Equal(42, value);
    }

    [Fact]
    public void NodeKind_Equality()
    {
        var kind1 = new NodeKind(1);
        var kind2 = new NodeKind(1);
        var kind3 = new NodeKind(2);
        Assert.Equal(kind1, kind2);
        Assert.NotEqual(kind1, kind3);
    }
}

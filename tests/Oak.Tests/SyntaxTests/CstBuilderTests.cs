using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;


/// <summary>

/// CstBuilder 具体语法树构建器测试


/// </summary>
public class CstBuilderTests
{
    [Fact]
    public void CstBuilder_SingleLeaf()
    {
        var b = new CstBuilder();
        b.add_token(1, "hello");
        var node = b.build();
        Assert.Equal(new NodeKind(1), node.kind);
        Assert.Equal(5, node.width);
        Assert.True(node.is_leaf);
    }

    [Fact]
    public void CstBuilder_SingleInternalNode()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.add_token(2, "abc");
        b.add_token(3, "de");
        b.end_node();
        var node = b.build();
        Assert.Equal(new NodeKind(1), node.kind);
        Assert.Equal(5, node.width);
        Assert.Equal(2, node.child_count);
    }

    [Fact]
    public void CstBuilder_NestedNodes()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.start_node(2);
        b.add_token(3, "x");
        b.end_node();
        b.add_token(4, "y");
        b.end_node();
        var node = b.build();
        Assert.Equal(new NodeKind(1), node.kind);
        Assert.Equal(2, node.width);
        Assert.Equal(2, node.child_count);
        var inner = node.get_child(0)!;
        Assert.Equal(new NodeKind(2), inner.kind);
        Assert.Equal(1, inner.width);
    }

    [Fact]
    public void CstBuilder_AddChild()
    {
        var childLeaf = new GreenLeafNode(10, 3, "abc");
        var b = new CstBuilder();
        b.start_node(1);
        b.add_child(childLeaf);
        b.end_node();
        var node = b.build();
        Assert.Equal(3, node.width);
        Assert.Same(childLeaf, node.get_child(0));
    }

    [Fact]
    public void CstBuilder_AddTokenWithTextSpan()
    {
        var b = new CstBuilder();
        b.add_token(1, default(TextSpan));
        var node = b.build();
        Assert.Equal(5, node.width);
    }
}

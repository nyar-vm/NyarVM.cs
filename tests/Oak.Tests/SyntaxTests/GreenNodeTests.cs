using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;


/// <summary>

/// GreenNode、GreenLeafNode、GreenInternalNode 绿树节点测试


/// </summary>
public class GreenNodeTests
{
    [Fact]
    public void GreenLeafNode_Properties()
    {
        var leaf = new GreenLeafNode(1, 5, "hello");
        Assert.Equal(new NodeKind(1), leaf.kind);
        Assert.Equal(5, leaf.width);
        Assert.Equal("hello", leaf.text);
        Assert.Equal(0, leaf.child_count);
        Assert.Null(leaf.get_child(0));
        Assert.True(leaf.is_leaf);
    }

    [Fact]
    public void GreenLeafNode_WithoutText()
    {
        var leaf = new GreenLeafNode(2, 3);
        Assert.Null(leaf.text);
        Assert.Equal(3, leaf.width);
    }

    [Fact]
    public void GreenInternalNode_Properties()
    {
        var child1 = new GreenLeafNode(1, 3, "abc");
        var child2 = new GreenLeafNode(2, 2, "de");
        var internalNode = new GreenInternalNode(3, [child1, child2]);
        Assert.Equal(new NodeKind(3), internalNode.kind);
        Assert.Equal(5, internalNode.width);
        Assert.Equal(2, internalNode.child_count);
        Assert.False(internalNode.is_leaf);
        Assert.Same(child1, internalNode.get_child(0));
        Assert.Same(child2, internalNode.get_child(1));
    }

    [Fact]
    public void GreenInternalNode_WidthIsSumOfChildren()
    {
        var child1 = new GreenLeafNode(1, 10);
        var child2 = new GreenLeafNode(2, 20);
        var parent = new GreenInternalNode(3, [child1, child2]);
        Assert.Equal(30, parent.width);
    }

    [Fact]
    public void GreenInternalNode_GetChild_OutOfRange_ReturnsNull()
    {
        var child = new GreenLeafNode(1, 5);
        var parent = new GreenInternalNode(2, [child]);
        Assert.Null(parent.get_child(-1));
        Assert.Null(parent.get_child(1));
    }

    [Fact]
    public void GreenNode_Children_Enumerable()
    {
        var child1 = new GreenLeafNode(1, 3);
        var child2 = new GreenLeafNode(2, 4);
        var parent = new GreenInternalNode(3, [child1, child2]);
        var children = parent.children.ToList();
        Assert.Equal(2, children.Count);
        Assert.Same(child1, children[0]);
        Assert.Same(child2, children[1]);
    }
}

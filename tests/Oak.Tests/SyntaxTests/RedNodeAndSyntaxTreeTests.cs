using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;


/// <summary>

/// RedNode 红树节点和 SyntaxTree 语法树测试


/// </summary>
public class RedNodeAndSyntaxTreeTests
{
    private static SyntaxTree CreateTestTree()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.add_token(2, "hello");
        b.add_token(3, " ");
        b.add_token(4, "world");
        b.end_node();
        var root = b.build();
        return new SyntaxTree(new StringSource("hello world"), root);
    }

    [Fact]
    public void SyntaxTree_GetRedRoot()
    {
        var tree = CreateTestTree();
        var redRoot = tree.get_red_root();
        Assert.Equal(new NodeKind(1), redRoot.kind);
        Assert.Equal(default(TextSpan), redRoot.span);
        Assert.Equal(3, redRoot.child_count);
    }

    [Fact]
    public void RedNode_GetChild()
    {
        var tree = CreateTestTree();
        var root = tree.get_red_root();
        var child0 = root.get_child(0);
        Assert.Equal(new NodeKind(2), child0.kind);
        Assert.Equal(default(TextSpan), child0.span);
        var child1 = root.get_child(1);
        Assert.Equal(new NodeKind(3), child1.kind);
        Assert.Equal(default(TextSpan), child1.span);
        var child2 = root.get_child(2);
        Assert.Equal(new NodeKind(4), child2.kind);
        Assert.Equal(default(TextSpan), child2.span);
    }

    [Fact]
    public void RedNode_Descendants()
    {
        var tree = CreateTestTree();
        var root = tree.get_red_root();
        var descendants = root.descendants().ToList();
        Assert.Equal(3, descendants.Count);
    }

    [Fact]
    public void SyntaxTree_Source()
    {
        var tree = CreateTestTree();
        Assert.Equal(11, tree.source.length);
    }
}

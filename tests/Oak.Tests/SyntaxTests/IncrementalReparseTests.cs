using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public class IncrementalReparseTests
{
    #region 辅助方法

    private static SyntaxTree CreateSimpleTree()
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

    private static SyntaxTree CreateNestedTree()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.start_node(2);
        b.add_token(3, "aaa");
        b.add_token(4, "bbb");
        b.end_node();
        b.start_node(5);
        b.add_token(6, "ccc");
        b.add_token(7, "ddd");
        b.end_node();
        b.end_node();
        var root = b.build();
        return new SyntaxTree(new StringSource("aaabbbcccddd"), root);
    }

    #endregion

    #region 单字符编辑测试

    [Fact]
    public void Edit_SingleCharInsertion_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();
        var originalRoot = tree.root;

        var edit = new Edit(default(TextSpan), "!");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.NotSame(tree, newTree);
        Assert.Equal("hello! world", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    [Fact]
    public void Edit_SingleCharDeletion_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal("helloworld", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    [Fact]
    public void Edit_SingleCharReplacement_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "H");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal("Hello world", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    #endregion

    #region 多行编辑测试

    [Fact]
    public void Edit_MultiLineInsertion_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "\nnew line\n");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal("hello\nnew line\n world", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    [Fact]
    public void Edit_MultiLineDeletion_ShouldProduceNewTree()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.add_token(2, "line1\n");
        b.add_token(3, "line2\n");
        b.add_token(4, "line3");
        b.end_node();
        var root = b.build();
        var tree = new SyntaxTree(new StringSource("line1\nline2\nline3"), root);

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal("line1\nline3", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    #endregion

    #region 删除整个节点测试

    [Fact]
    public void Edit_DeleteEntireToken_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal(" world", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    [Fact]
    public void Edit_DeleteAllContent_ShouldProduceEmptyTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal("", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    #endregion

    #region 插入新节点测试

    [Fact]
    public void Edit_InsertAtBeginning_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "prefix ");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal("prefix hello world", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    [Fact]
    public void Edit_InsertAtEnd_ShouldProduceNewTree()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), " suffix");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal("hello worl suffix", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    #endregion

    #region 增量解析器注册测试

    [Fact]
    public void Edit_WithIncrementalParser_ShouldUseRegisteredParser()
    {
        var tree = CreateSimpleTree();

        var wasCalled = false;
        var repo = new IncrementalParserRepo();
        repo.register(new NodeKind(1), (source, span, context, out changed) =>
        {
            wasCalled = true;
            changed = true;
            return new GreenInternalNode(1, [
                new GreenLeafNode(2, 5, "HELLO"),
                new GreenLeafNode(3, 1, " "),
                new GreenLeafNode(4, 5, "WORLD")
            ]);
        });

        var edit = new Edit(default(TextSpan), "H");
        var newTree = tree.edit(edit, repo);

        Assert.True(wasCalled);
    }

    [Fact]
    public void Edit_WithoutIncrementalParser_ShouldStillApplyEdit()
    {
        var tree = CreateSimpleTree();

        var repo = new IncrementalParserRepo();
        var edit = new Edit(default(TextSpan), "H");
        var newTree = tree.edit(edit, repo);

        Assert.Equal("Hello world", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    #endregion

    #region 未变部分共享测试（引用相等性）

    [Fact]
    public void Edit_UnchangedLeaf_ShouldBeSharedByReference()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.start_node(2);
        b.add_token(3, "aaa");
        b.end_node();
        b.start_node(4);
        b.add_token(5, "bbb");
        b.end_node();
        b.end_node();
        var root = b.build();
        var tree = new SyntaxTree(new StringSource("aaabbb"), root);

        var originalSecondChild = root.get_child(1);

        var edit = new Edit(default(TextSpan), "A");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        var newSecondChild = newTree.root.get_child(1);
        Assert.Same(originalSecondChild, newSecondChild);
    }

    [Fact]
    public void Edit_MultipleEdits_UnchangedPartsShouldRemainShared()
    {
        var tree = CreateSimpleTree();
        var originalRoot = tree.root;

        var repo = new IncrementalParserRepo();

        var edit1 = new Edit(default(TextSpan), "H");
        var tree2 = tree.edit(edit1, repo);

        var edit2 = new Edit(default(TextSpan), "W");
        var tree3 = tree2.edit(edit2, repo);

        Assert.Equal("Hello World", tree3.source.substring(new Range(0, tree3.source.length)));
    }

    #endregion

    #region TreeChangeEvent 测试

    [Fact]
    public void Edit_ShouldProduceTreeChangeEvent_WhenRootChanges()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "H");
        var repo = new IncrementalParserRepo();
        repo.register(new NodeKind(1), (source, span, context, out changed) =>
        {
            changed = true;
            return new GreenInternalNode(1, [
                new GreenLeafNode(2, 1, "H"),
                new GreenLeafNode(3, 1, " "),
                new GreenLeafNode(4, 5, "world")
            ]);
        });
        var newTree = tree.edit(edit, repo);

        Assert.NotSame(tree, newTree);
        Assert.NotSame(tree.root, newTree.root);
    }

    [Fact]
    public void Edit_Delta_ShouldBePositiveForInsertion()
    {
        var edit = new Edit(default(TextSpan), "inserted");
        Assert.Equal(8, edit.delta);
    }

    [Fact]
    public void Edit_Delta_ShouldBeNegativeForDeletion()
    {
        var edit = new Edit(default(TextSpan), "");
        Assert.Equal(-3, edit.delta);
    }

    [Fact]
    public void Edit_Delta_ShouldBeZeroForReplacement()
    {
        var edit = new Edit(default(TextSpan), "abc");
        Assert.Equal(0, edit.delta);
    }

    #endregion

    #region 边界情况测试

    [Fact]
    public void Edit_AtExactBoundary_ShouldWork()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "!");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal("hello worl!", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    [Fact]
    public void Edit_EmptyEdit_ShouldProduceSameSource()
    {
        var tree = CreateSimpleTree();

        var edit = new Edit(default(TextSpan), "");
        var repo = new IncrementalParserRepo();
        var newTree = tree.edit(edit, repo);

        Assert.Equal("hello world", newTree.source.substring(new Range(0, newTree.source.length)));
    }

    #endregion
}

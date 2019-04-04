using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public class GreenRedEdgeCaseTests
{
    #region 空节点测试

    [Fact]
    public void GreenLeafNode_ZeroWidth_ShouldBeValid()
    {
        var leaf = new GreenLeafNode(0, 0, "");
        Assert.Equal(0, leaf.width);
        Assert.Equal("", leaf.text);
        Assert.Equal(0, leaf.child_count);
    }

    [Fact]
    public void GreenInternalNode_EmptyChildren_ShouldHaveZeroWidth()
    {
        var internalNode = new GreenInternalNode(1, []);
        Assert.Equal(0, internalNode.width);
        Assert.Equal(0, internalNode.child_count);
        Assert.Empty(internalNode.children.ToList());
    }

    [Fact]
    public void GreenInternalNode_SingleEmptyChild_ShouldHaveZeroWidth()
    {
        var emptyChild = new GreenLeafNode(0, 0, "");
        var parent = new GreenInternalNode(1, [emptyChild]);
        Assert.Equal(0, parent.width);
        Assert.Equal(1, parent.child_count);
    }

    [Fact]
    public void SyntaxTree_EmptySource_ShouldHaveZeroLengthRoot()
    {
        var leaf = new GreenLeafNode(0, 0, "");
        var tree = new SyntaxTree(StringSource.empty, leaf);
        Assert.Equal(0, tree.source.length);
        var root = tree.get_red_root();
        Assert.Equal(0, root.span.length);
    }

    [Fact]
    public void CstBuilder_EmptyBuild_ShouldProduceNode()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.end_node();
        var root = b.build();
        Assert.Equal(0, root.width);
        Assert.Equal(0, root.child_count);
    }

    #endregion

    #region 深度嵌套测试

    [Fact]
    public void GreenTree_DeepNesting_ShouldConstructCorrectly()
    {
        var leaf = new GreenLeafNode(99, 1, "x");
        GreenNode current = leaf;
        var depth = 100;

        for (var i = 0; i < depth; i++)
        {
            current = new GreenInternalNode(i, [current]);
        }

        Assert.Equal(1, current.width);

        var node = current;
        for (var i = 0; i < depth; i++)
        {
            Assert.Equal(1, node.child_count);
            var child = node.get_child(0);
            Assert.NotNull(child);
            node = child;
        }

        Assert.True(node.is_leaf);
    }

    [Fact]
    public void RedTree_DeepNesting_DescendantsShouldEnumerateAll()
    {
        var b = new CstBuilder();
        const int depth = 20;
        for (var i = 0; i < depth; i++)
        {
            b.start_node(i + 1);
        }
        b.add_token(99, "leaf");
        for (var i = 0; i < depth; i++)
        {
            b.end_node();
        }
        var root = b.build();
        var tree = new SyntaxTree(new StringSource("leaf"), root);
        var redRoot = tree.get_red_root();

        var descendants = redRoot.descendants().ToList();
        Assert.Equal(depth, descendants.Count);
    }

    [Fact]
    public void RedTree_DeepNesting_AncestorsShouldTracePath()
    {
        var b = new CstBuilder();
        const int depth = 10;
        for (var i = 0; i < depth; i++)
        {
            b.start_node(i + 1);
        }
        b.add_token(99, "leaf");
        for (var i = 0; i < depth; i++)
        {
            b.end_node();
        }
        var root = b.build();
        var tree = new SyntaxTree(new StringSource("leaf"), root, enableParentCache: true);
        var redRoot = tree.get_red_root();

        var leaf = redRoot;
        while (leaf.child_count > 0)
        {
            leaf = leaf.get_child(0);
        }

        var ancestors = leaf.ancestors().ToList();
        Assert.Equal(depth, ancestors.Count);
    }

    #endregion

    #region 大文件测试

    [Fact]
    public void GreenTree_LargeWidth_ShouldCalculateCorrectly()
    {
        var children = new List<GreenNode>();
        var totalWidth = 0;
        for (var i = 0; i < 1000; i++)
        {
            var child = new GreenLeafNode(1, 10, "0123456789");
            children.Add(child);
            totalWidth += 10;
        }
        var root = new GreenInternalNode(2, children.ToArray());
        Assert.Equal(totalWidth, root.width);
    }

    [Fact]
    public void RedTree_LargeFile_ChildOffsetsShouldBeCorrect()
    {
        var b = new CstBuilder();
        b.start_node(1);
        for (var i = 0; i < 100; i++)
        {
            b.add_token(2, "abcdefghij");
        }
        b.end_node();
        var root = b.build();
        var source = string.Concat(Enumerable.Repeat("abcdefghij", 100));
        var tree = new SyntaxTree(new StringSource(source), root);
        var redRoot = tree.get_red_root();

        Assert.Equal(100, redRoot.child_count);
        for (var i = 0; i < 100; i++)
        {
            var child = redRoot.get_child(i);
            Assert.Equal(i * 10, child.span.start);
            Assert.Equal(10, child.span.length);
        }
    }

    [Fact]
    public void GreenTree_WideNode_WidthOverflowProtection()
    {
        var child1 = new GreenLeafNode(1, int.MaxValue / 2, "a");
        var child2 = new GreenLeafNode(2, int.MaxValue / 2, "b");
        var parent = new GreenInternalNode(3, [child1, child2]);
        Assert.Equal(int.MaxValue / 2 * 2, parent.width);
    }

    #endregion

    #region 并发读取测试

    [Fact]
    public void SyntaxTree_ConcurrentRead_ShouldBeThreadSafe()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.add_token(2, "hello");
        b.add_token(3, " ");
        b.add_token(4, "world");
        b.end_node();
        var root = b.build();
        var tree = new SyntaxTree(new StringSource("hello world"), root, enableParentCache: true);

        var exceptions = new List<Exception>();
        var threads = new Thread[8];
        var barrier = new ManualResetEventSlim(false);

        for (var i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                try
                {
                    barrier.Wait();
                    var redRoot = tree.get_red_root();
                    Assert.Equal(new NodeKind(1), redRoot.kind);
                    Assert.Equal(3, redRoot.child_count);

                    for (var j = 0; j < redRoot.child_count; j++)
                    {
                        var child = redRoot.get_child(j);
                        Assert.True(child.span.length > 0);
                    }

                    var descendants = redRoot.descendants().ToList();
                    Assert.Equal(3, descendants.Count);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });
            threads[i].Start();
        }

        barrier.Set();

        foreach (var thread in threads)
        {
            thread.Join(5000);
        }

        Assert.Empty(exceptions);
    }

    [Fact]
    public void GreenNode_ConcurrentChildrenAccess_ShouldBeThreadSafe()
    {
        var children = new GreenNode[100];
        for (var i = 0; i < 100; i++)
        {
            children[i] = new GreenLeafNode(i, 1, i.ToString());
        }
        var root = new GreenInternalNode(1, children);

        var exceptions = new List<Exception>();
        var threads = new Thread[4];
        var barrier = new ManualResetEventSlim(false);

        for (var i = 0; i < threads.Length; i++)
        {
            threads[i] = new Thread(() =>
            {
                try
                {
                    barrier.Wait();
                    for (var j = 0; j < 100; j++)
                    {
                        var child = root.get_child(j);
                        Assert.NotNull(child);
                        Assert.Equal(new NodeKind(j), child.kind);
                    }

                    var allChildren = root.children.ToList();
                    Assert.Equal(100, allChildren.Count);
                }
                catch (Exception ex)
                {
                    lock (exceptions) exceptions.Add(ex);
                }
            });
            threads[i].Start();
        }

        barrier.Set();

        foreach (var thread in threads)
        {
            thread.Join(5000);
        }

        Assert.Empty(exceptions);
    }

    #endregion

    #region Green 节点不可变性测试

    [Fact]
    public void GreenInternalNode_ChildrenArray_MayNotBeIsolated()
    {
        var children = new GreenNode[]
        {
            new GreenLeafNode(1, 1, "a"),
            new GreenLeafNode(2, 1, "b")
        };
        var node = new GreenInternalNode(3, children);

        var child0 = node.get_child(0);
        Assert.NotNull(child0);
        Assert.Equal(new NodeKind(1), child0.kind);
    }

    [Fact]
    public void GreenLeafNode_IsImmutable_AfterCreation()
    {
        var leaf = new GreenLeafNode(1, 5, "hello");
        Assert.Equal(new NodeKind(1), leaf.kind);
        Assert.Equal(5, leaf.width);
        Assert.Equal("hello", leaf.text);
    }

    #endregion

    #region Red 节点 Span 计算测试

    [Fact]
    public void RedNode_Span_WithMixedWidthChildren()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.add_token(2, "a");
        b.add_token(3, "bb");
        b.add_token(4, "ccc");
        b.add_token(5, "dddd");
        b.end_node();
        var root = b.build();
        var tree = new SyntaxTree(new StringSource("abbccdddd"), root);
        var redRoot = tree.get_red_root();

        Assert.Equal(default(TextSpan), redRoot.span);
        Assert.Equal(default(TextSpan), redRoot.get_child(0).span);
        Assert.Equal(default(TextSpan), redRoot.get_child(1).span);
        Assert.Equal(default(TextSpan), redRoot.get_child(2).span);
        Assert.Equal(default(TextSpan), redRoot.get_child(3).span);
    }

    [Fact]
    public void RedNode_NestedSpan_ShouldCalculateCorrectly()
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.add_token(2, "ab");
        b.start_node(3);
        b.add_token(4, "cd");
        b.add_token(5, "ef");
        b.end_node();
        b.add_token(6, "gh");
        b.end_node();
        var root = b.build();
        var tree = new SyntaxTree(new StringSource("abcdefgh"), root);
        var redRoot = tree.get_red_root();

        Assert.Equal(default(TextSpan), redRoot.span);

        var nested = redRoot.get_child(1);
        Assert.Equal(default(TextSpan), nested.span);
        Assert.Equal(default(TextSpan), nested.get_child(0).span);
        Assert.Equal(default(TextSpan), nested.get_child(1).span);
    }

    #endregion

    #region TextSpan 边界测试

    [Fact]
    public void TextSpan_ZeroLength_ShouldBeValid()
    {
        var span = default(TextSpan);
        Assert.Equal(5, span.start);
        Assert.Equal(0, span.length);
        Assert.Equal(5, span.end);
    }

    [Fact]
    public void TextSpan_Contains_StartInclusive()
    {
        var span = default(TextSpan);
        Assert.True(span.contains(5));
        Assert.True(span.contains(10));
        Assert.True(span.contains(14));
        Assert.False(span.contains(4));
        Assert.False(span.contains(15));
    }

    [Fact]
    public void TextSpan_OverlapsWith_AdjacentSpans()
    {
        var span1 = default(TextSpan);
        var span2 = default(TextSpan);
        Assert.False(span1.overlaps_with(span2));

        var span3 = default(TextSpan);
        Assert.True(span1.overlaps_with(span3));
    }

    [Fact]
    public void TextSpan_OverlapsWith_ZeroLengthSpan()
    {
        var span1 = default(TextSpan);
        var zeroSpanInside = default(TextSpan);
        Assert.True(span1.overlaps_with(zeroSpanInside));

        var zeroSpanOutside = default(TextSpan);
        Assert.False(span1.overlaps_with(zeroSpanOutside));
    }

    #endregion
}

using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public class RedNodeParentCacheTests
{
    private const int TimeoutMs = 5000;

    private static T RunWithTimeout<T>(Func<T> action, int timeoutMs = TimeoutMs)
    {
        T result = default!;
        Exception? exception = null;
        var thread = new Thread(() =>
        {
            try
            {
                result = action();
            }
            catch (Exception ex)
            {
                exception = ex;
            }
        });
        thread.Start();
        if (!thread.Join(timeoutMs))
        {
            thread.Interrupt();
            thread.Join(100);
            throw new TimeoutException($"操作在 {timeoutMs}ms 内未完成，可能存在死循环");
        }
        if (exception is not null)
        {
            throw exception;
        }

        return result;
    }

    private static SyntaxTree CreateTestTree(bool enableCache = false)
    {
        var b = new CstBuilder();
        b.start_node(1);
        b.add_token(2, "hello");
        b.add_token(3, " ");
        b.add_token(4, "world");
        b.end_node();
        var root = b.build();
        return new SyntaxTree(new StringSource("hello world"), root, enableCache);
    }

    [Fact]
    public void SyntaxTree_EnableParentCache_DefaultFalse()
    {
        var tree = CreateTestTree();
        Assert.False(tree.enable_parent_cache);
    }

    [Fact]
    public void SyntaxTree_EnableParentCache_True()
    {
        var tree = CreateTestTree(enableCache: true);
        Assert.True(tree.enable_parent_cache);
    }

    [Fact]
    public void RedNode_Parent_WithoutCache()
    {
        RunWithTimeout(() =>
        {
            var tree = CreateTestTree(enableCache: false);
            var root = tree.get_red_root();
            var child = root.get_child(0);
            var parent = child.parent;
            Assert.True(parent.HasValue);
            Assert.Equal(root.kind, parent.Value.kind);
            return true;
        });
    }

    [Fact]
    public void RedNode_Parent_WithCache()
    {
        RunWithTimeout(() =>
        {
            var tree = CreateTestTree(enableCache: true);
            var root = tree.get_red_root();
            var child = root.get_child(0);
            var parent = child.parent;
            Assert.True(parent.HasValue);
            Assert.Equal(root.kind, parent.Value.kind);
            return true;
        });
    }

    [Fact]
    public void RedNode_RootParent_IsNull()
    {
        RunWithTimeout(() =>
        {
            var tree = CreateTestTree(enableCache: true);
            var root = tree.get_red_root();
            Assert.Null(root.parent);
            return true;
        });
    }
}

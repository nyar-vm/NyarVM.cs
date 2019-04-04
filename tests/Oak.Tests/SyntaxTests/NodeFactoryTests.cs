using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;

public class NodeFactoryTests
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

    [Fact]
    public void NodeFactory_RegisterAndGet()
    {
        RunWithTimeout(() =>
        {
            NodeFactory.register(100, (green, tree, offset) => new TestSyntaxRoot(green, tree, offset, "test"));
            var factory = NodeFactory.get(100);
            Assert.NotNull(factory);
            NodeFactory.clear();
            return true;
        });
    }

    [Fact]
    public void NodeFactory_GetUnregistered_ReturnsNull()
    {
        RunWithTimeout(() =>
        {
            NodeFactory.clear();
            var factory = NodeFactory.get(999);
            Assert.Null(factory);
            return true;
        });
    }

    [Fact]
    public void NodeFactory_Create()
    {
        RunWithTimeout(() =>
        {
            NodeFactory.register(100, (green, tree, offset) => new TestSyntaxRoot(green, tree, offset, "test"));
            var leaf = new GreenLeafNode(100, 5, "hello");
            var source = new StringSource("hello");
            var tree = new SyntaxTree(source, leaf);
            var node = NodeFactory.create(100, leaf, tree, 0);
            Assert.NotNull(node);
            Assert.IsType<TestSyntaxRoot>(node);
            NodeFactory.clear();
            return true;
        });
    }

    [Fact]
    public void NodeFactory_IsRegistered()
    {
        RunWithTimeout(() =>
        {
            NodeFactory.clear();
            Assert.False(NodeFactory.is_registered(100));
            NodeFactory.register(100, (green, tree, offset) => new TestSyntaxRoot(green, tree, offset, "test"));
            Assert.True(NodeFactory.is_registered(100));
            NodeFactory.clear();
            return true;
        });
    }

    private sealed class TestSyntaxRoot : SyntaxRoot
    {
        public TestSyntaxRoot(GreenNode green, SyntaxTree tree, int offset, string languageId)
            : base(green, tree, offset, languageId) { }
        public override VisitRecursionMode accept(SyntaxVisitor visitor) => visitor.visit_default(this);
    }
}

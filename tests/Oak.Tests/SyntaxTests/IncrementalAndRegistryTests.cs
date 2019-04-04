using Oak.Syntax;

namespace Oak.Core.Tests.SyntaxTests;


/// <summary>

/// IncrementalParserRepo、TreeChangeEvent、LanguageRegistry 增量解析与注册表测试


/// </summary>
public class IncrementalAndRegistryTests
{
    [Fact]
    public void IncrementalParserRepo_RegisterAndGet()
    {
        var repo = new IncrementalParserRepo();
        IncrementalParser parser = (ISource source, TextSpan span, ISyntaxContext? ctx, out bool changed) =>
        {
            changed = false;
            return null;
        };
        repo.register(new NodeKind(1), parser);
        var result = repo.get(new NodeKind(1));
        Assert.NotNull(result);
    }

    [Fact]
    public void IncrementalParserRepo_GetUnregistered_ReturnsNull()
    {
        var repo = new IncrementalParserRepo();
        var result = repo.get(new NodeKind(99));
        Assert.Null(result);
    }

    [Fact]
    public void TreeChangeEvent_Properties()
    {
        var source = new StringSource("hello");
        var b = new CstBuilder();
        b.add_token(1, "hello");
        var root = b.build();
        var oldTree = new SyntaxTree(source, root);
        var newTree = new SyntaxTree(source, root);
        var changedSpan = default(TextSpan);
        var replaced = new List<GreenNode> { root }.AsReadOnly();

        var change = new TreeChangeEvent(oldTree, newTree, changedSpan, replaced, new Edit(changedSpan, "hello"));
        Assert.Same(oldTree, change.old_tree);
        Assert.Same(newTree, change.new_tree);
        Assert.Equal(changedSpan, change.changed_span);
        Assert.Single(change.replaced_nodes);
    }

    [Fact]
    public void LanguageRegistry_RegisterAndParse()
    {
        LanguageRegistry.clear();
        var source = new StringSource("test");
        var lang = new TestLang();
        LanguageRegistry.register("test-lang", lang, s =>
        {
            var builder = new CstBuilder();
            builder.add_token(1, "test");
            var green = builder.build();
            var tree = new SyntaxTree(s, green);
            return new TestSyntaxRoot(green, tree, 0, "test-lang");
        });
        Assert.True(LanguageRegistry.is_registered("test-lang"));
        var result = LanguageRegistry.parse("test-lang", source);
        Assert.NotNull(result);
        LanguageRegistry.clear();
    }

    [Fact]
    public void LanguageRegistry_Unregistered_Throws()
    {
        LanguageRegistry.clear();
        Assert.Throws<InvalidOperationException>(() => LanguageRegistry.parse("nonexistent", new StringSource("")));
    }

    private sealed class TestLang : Language
    {
        public override string name => "TestLang";
    }

    private sealed class TestSyntaxRoot : SyntaxRoot
    {
        public TestSyntaxRoot(GreenNode green, SyntaxTree tree, int offset, string languageId)
            : base(green, tree, offset, languageId) { }
        public override VisitRecursionMode accept(SyntaxVisitor visitor) => visitor.visit_default(this);
    }
}

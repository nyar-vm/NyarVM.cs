using Nyar.Analyzer.Semantic;

namespace Valkyrie.Tests.LanguageTests;

public sealed class RowTypeCompatibilityTests
{
    [Fact]
    public void OpenRow_AcceptsAdditionalMethods()
    {
        var rowType = new RowType([Method("get_value", new FunctionType([], new PrimitiveType("i32")))], isOpen: true);
        var candidate = new NamedType("Box", "class", members:
        [
            Method("get_value", new FunctionType([], new PrimitiveType("i32"))),
            Method("set_value", new FunctionType([new PrimitiveType("i32")], new PrimitiveType("unit")))
        ]);

        Assert.True(rowType.is_assignable_from(candidate));
    }

    [Fact]
    public void ClosedRow_RejectsAdditionalMethods()
    {
        var rowType = new RowType([Method("get_value", new FunctionType([], new PrimitiveType("i32")))], isOpen: false);
        var candidate = new NamedType("Box", "class", members:
        [
            Method("get_value", new FunctionType([], new PrimitiveType("i32"))),
            Method("set_value", new FunctionType([new PrimitiveType("i32")], new PrimitiveType("unit")))
        ]);

        Assert.False(rowType.is_assignable_from(candidate));
    }

    [Fact]
    public void TraitLikeNamedType_UsesMethodRowsForCompatibility()
    {
        var traitType = new NamedType("Readable", "trait", members:
        [
            Method("get_value", new FunctionType([], new PrimitiveType("i32")))
        ]);
        var candidate = new NamedType("Box", "class", members:
        [
            new Symbol("value", SymbolKind.property, SymbolAccessibility.@public, new PrimitiveType("i32")),
            Method("get_value", new FunctionType([], new PrimitiveType("i32"))),
            Method("set_value", new FunctionType([new PrimitiveType("i32")], new PrimitiveType("unit")))
        ]);

        Assert.True(traitType.is_assignable_from(candidate));
    }

    private static Symbol Method(string name, IType type)
    {
        return new Symbol(name, SymbolKind.method, SymbolAccessibility.@public, type);
    }
}

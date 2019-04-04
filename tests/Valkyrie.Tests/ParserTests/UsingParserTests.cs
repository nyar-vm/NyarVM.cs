namespace Valkyrie.Tests.ParserTests;

public class UsingParserTests : ValkyrieParserTestBase
{
    [Fact]
    public void Parse_SimpleUsing_ShouldSucceed()
    {
        var source = "using Gameplay.Sonic.Core;";
        var unit = parse_with_timeout(source);
        var usingDecl = Assert.IsType<DeclareUsing>(unit.declarations[0]);

        Assert.Equal("Gameplay.Sonic.Core", usingDecl.module_path);
        Assert.Null(usingDecl.alias);
        Assert.False(usingDecl.is_reexport);
        Assert.Empty(usingDecl.selections);
    }

    [Fact]
    public void Parse_UsingWithAlias_ShouldSucceed()
    {
        var source = "using Gameplay.Sonic.Core as core;";
        var unit = parse_with_timeout(source);
        var usingDecl = Assert.IsType<DeclareUsing>(unit.declarations[0]);

        Assert.Equal("Gameplay.Sonic.Core", usingDecl.module_path);
        Assert.Equal("core", usingDecl.alias?.name);
    }

    [Fact]
    public void Parse_MultipleUsings_ShouldSucceed()
    {
        var source = """
                     using Math;
                     using Physics as phys;
                     using UI;
                     """;
        var unit = parse_with_timeout(source);

        Assert.Equal(3, unit.declarations.Count);

        var u1 = Assert.IsType<DeclareUsing>(unit.declarations[0]);
        Assert.Equal("Math", u1.module_path);

        var u2 = Assert.IsType<DeclareUsing>(unit.declarations[1]);
        Assert.Equal("Physics", u2.module_path);
        Assert.Equal("phys", u2.alias?.name);

        var u3 = Assert.IsType<DeclareUsing>(unit.declarations[2]);
        Assert.Equal("UI", u3.module_path);
    }

    [Fact]
    public void Parse_ReexportUsingWithSelections_ShouldSucceed()
    {
        var source = "using! std.iterator.{IntoIterator, Iterator};";
        var unit = parse_with_timeout(source);
        var usingDecl = Assert.IsType<DeclareUsing>(unit.declarations[0]);

        Assert.True(usingDecl.is_reexport);
        Assert.Equal("std.iterator", usingDecl.module_path);
        Assert.Equal(["IntoIterator", "Iterator"], usingDecl.selections.Select(item => item.name).ToArray());
    }
}

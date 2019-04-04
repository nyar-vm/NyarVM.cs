using Nyar.Language.Valkyrie.Compiler.Hir;
using Xunit;

namespace Valkyrie.Tests.CompilerTests;

public sealed class ClrImportAttributeTests
{
    [Fact]
    public void CollectCallableExternalImportLinks_WithClrMethodAttribute_ShouldBindMethodImport()
    {
        var attributes = new[]
        {
            new HirAttribute("clr", ["System.Console", "System.Console", "WriteLine"])
        };

        var link = Assert.Single(HirAttributeSemantics.collect_callable_external_import_links(attributes));
        var clrLink = Assert.IsType<HirClrMethodImportLink>(link);
        Assert.Equal("System.Console", clrLink.assembly_name);
        Assert.Equal("System.Console", clrLink.type_full_name);
        Assert.Equal("WriteLine", clrLink.method_name);
    }

    [Fact]
    public void CollectTypeExternalImportLinks_WithClrTypeAttribute_ShouldBindTypeImport()
    {
        var attributes = new[]
        {
            new HirAttribute("clr", ["System.Runtime", "System.String"])
        };

        var link = Assert.Single(HirAttributeSemantics.collect_type_external_import_links(attributes));
        var clrLink = Assert.IsType<HirClrTypeImportLink>(link);
        Assert.Equal("System.Runtime", clrLink.assembly_name);
        Assert.Equal("System.String", clrLink.type_full_name);
    }

    [Fact]
    public void CollectCallableExternalImportLinks_WithClrTypeAttribute_ShouldThrow()
    {
        var attributes = new[]
        {
            new HirAttribute("clr", ["System.Runtime", "System.String"])
        };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            HirAttributeSemantics.collect_callable_external_import_links(attributes));
        Assert.Contains("函数上的 `[clr]` 需要 3 个参数", ex.Message, StringComparison.Ordinal);
    }
}

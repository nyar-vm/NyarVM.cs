using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

public sealed class UniteHirTests
{
    [Fact]
    public void BuildHir_UniteVariantWithMultipleFields_ShouldPreserveFieldsAndProductPayload()
    {
        const string source = """
                              namespace app;

                              [unite]
                              unite PairOrNone {
                                  Pair { left: i32, right: string },
                                  None
                              }
                              """;

        var module = build_hir(source);
        var uniteType = module.types.Single(type => type.name == "PairOrNone");
        var pairVariant = Assert.Single(uniteType.variants!.Where(variant => variant.name == "Pair"));

        Assert.Equal(HirTypeKind.unite, uniteType.kind);
        Assert.Equal("i32 * string", pairVariant.payload_type?.name);
        Assert.Collection(pairVariant.fields,
            field =>
            {
                Assert.Equal("left", field.name);
                Assert.Equal("i32", field.type.name);
            },
            field =>
            {
                Assert.Equal("right", field.name);
                Assert.Equal("string", field.type.name);
            });
    }

    private static HirModule build_hir(string source)
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("unite_hir", "jvm-openjdk-linux-managed", "unite_hir.v");
        var parseResult = compiler.parse_source(source, "unite_hir.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));
        return compiler.build_hir(stagedAst, semantics, plan);
    }
}

using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

public sealed class HirAliasTests
{
    [Fact]
    public void BuildHir_TypeAlias_ShouldExposeAliasTarget()
    {
        const string source = """
                              namespace app;

                              structure User {
                                  name: utf8
                              }

                              type UserAlias = User;
                              type VecIter = std.collection.ArrayIterator<i32>;
                              """;

        var module = build_hir(source, "type_alias.v");
        var userAliasType = Assert.NotNull(module.find_data_type(HirNamePath.parse("app.UserAlias")));
        Assert.Equal("User", userAliasType.name);

        var alias = Assert.NotNull(module.resolve_type_alias(HirNamePath.parse("app.VecIter")));
        Assert.Equal("VecIter", alias.name);
        Assert.Equal("std.collection.ArrayIterator", alias.target_type.name);
        Assert.Single(alias.target_type.type_arguments!);
        Assert.Equal("i32", alias.target_type.type_arguments![0].name);
    }

    [Fact]
    public void BuildHir_TraitAlias_ShouldFlattenCompositeTargets()
    {
        const string source = """
                              namespace app;

                              trait Readable {
                              }

                              trait Writable {
                              }

                              trait IO = Readable + Writable;
                              """;

        var module = build_hir(source, "trait_alias.v");
        var alias = Assert.NotNull(module.resolve_trait_alias(HirNamePath.parse("app.IO")));
        Assert.Equal("IO", alias.name);
        Assert.Equal(2, alias.target_traits.Count);
        Assert.Contains(alias.target_traits, type => type.name == "Readable");
        Assert.Contains(alias.target_traits, type => type.name == "Writable");
    }

    private static HirModule build_hir(string source, string fileName)
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("hir_alias", "jvm-openjdk-linux-managed", fileName);
        var parseResult = compiler.parse_source(source, fileName);

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));
        return compiler.build_hir(stagedAst, semantics, plan);
    }
}

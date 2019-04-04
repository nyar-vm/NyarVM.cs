using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

public sealed class HirReturnInferenceTests
{
    [Fact]
    public void BuildHir_NamespacedNestedCall_ShouldPreserveInferredReturnType()
    {
        const string source = """
                              namespace hello_world;

                              micro add_one(x: isize) {
                                  let one = 1;
                                  return x + one;
                              }

                              micro add_two(x: isize) {
                                  return add_one(add_one(x));
                              }
                              """;

        var module = build_hir(source);
        var addTwo = module.functions.Single(function => function.name == "hello_world.add_two");

        Assert.Equal("isize", addTwo.return_type.name);
    }

    [Fact]
    public void BuildHir_TextLiteralWithoutAnnotation_ShouldDefaultReturnTypeToUtf8()
    {
        const string source = """
                              namespace hello_world;

                              micro greet() {
                                  return "hello";
                              }
                              """;

        var module = build_hir(source);
        var greet = module.functions.Single(function => function.name == "hello_world.greet");

        Assert.Equal("utf8", greet.return_type.name);
    }

    private static HirModule build_hir(string source)
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("hir_return_inference", "jvm-openjdk-linux-managed", "hir_return_inference.v");
        var parseResult = compiler.parse_source(source, "hir_return_inference.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));
        return compiler.build_hir(stagedAst, semantics, plan);
    }
}

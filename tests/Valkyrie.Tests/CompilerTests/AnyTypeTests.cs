using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class AnyTypeTests
{
    [Fact]
    public void Analyze_ExplicitAnyType_ShouldAllowConcreteArgumentsAndReturns()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("any_type", "jvm-openjdk-linux-managed", "any_type.v");
        var parseResult = compiler.parse_source(
            """
            namespace app;

            class Box {
            }

            micro echo(value: any) -> any {
                return value;
            }

            [main]
            micro main() -> ExitCode {
                let box: any = Box {};
                let echoed: any = echo(box);
                return ExitCode(0 as i32);
            }
            """,
            "any_type.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);

        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));
    }

    [Fact]
    public void BuildLir_ExplicitAnyType_ShouldPreserveAnySignature()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("any_type", "jvm-openjdk-linux-managed", "any_type.v");
        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var parseResult = compiler.parse_source(
            """
            namespace app;

            class Box {
            }

            micro echo(value: any) -> any {
                return value;
            }

            [main]
            micro main() -> ExitCode {
                let box: any = Box {};
                let echoed: any = echo(box);
                return ExitCode(0 as i32);
            }
            """,
            "any_type.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);
        var echo = Assert.Single(lir.module.Functions, function => function.Name == "app.echo");

        Assert.Equal("any", echo.ReturnType);
        Assert.Collection(echo.Parameters,
            parameter =>
            {
                Assert.Equal("value", parameter.Name);
                Assert.Equal("any", parameter.type);
            });
    }
}

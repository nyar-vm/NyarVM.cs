using Std.Data.Binary.NyarIR.Data;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class IntegerLoweringTests
{
    [Fact]
    public void BuildLir_IsizeParameterAndReturn_ShouldLowerToIntegerSignature()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("integer_lowering", "jvm-openjdk-linux-managed", "integer_lowering.v");
        var lir = build_lir(
            """
            namespace app;

            [main]
            micro main() -> Unit {
                let result = add_one(41);
            }

            micro add_one(x: isize) {
                let one = 1;
                return x + one;
            }
            """,
            "integer_lowering.v");
        var addOne = Assert.Single(lir.module.functions, function => function.Name == "app.add_one");

        Assert.Collection(addOne.Parameters,
            parameter =>
            {
                Assert.Equal("x", parameter.Name);
                Assert.Equal("i32", parameter.type);
            });
        Assert.Equal("i32", addOne.ReturnType);
        Assert.Contains(addOne.Instructions, instruction => instruction.Opcode == NyarHeadCode.I32Add);
    }

    [Fact]
    public void BuildLir_MainImplicitExitCode_ShouldLowerToIntegerReturn()
    {
        var lir = build_lir(
            """
            namespace app;

            [main]
            micro main() {
                ExitCode(7 as i32)
            }
            """,
            "main_exit_code_lowering.v");
        var main = Assert.Single(lir.module.Functions, function => function.Name == "app.main");

        Assert.Equal("i32", main.ReturnType);
    }

    private static global::Nyar.Language.Valkyrie.Compiler.Lir.LirModule build_lir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("integer_lowering", "jvm-openjdk-linux-managed", fileName);
        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var parseResult = compiler.parse_source(source, fileName);

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        return compiler.build_lir(mir, plan);
    }
}

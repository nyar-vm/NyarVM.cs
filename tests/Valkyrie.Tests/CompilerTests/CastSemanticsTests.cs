using Std.Data.Binary.NyarIR.Data;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class CastSemanticsTests
{
    [Fact]
    public void Analyze_StringToI32Cast_ShouldReportSemanticDiagnostic()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("cast_semantics", "jvm-openjdk-linux-managed", "invalid_cast.v");
        var parseResult = compiler.parse_source(
            """
            namespace app;

            [main]
            micro main() -> ExitCode {
                let value = "42" as i32;
                return ExitCode(0 as i32);
            }
            """,
            "invalid_cast.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);

        var diagnostic = Assert.Single(semantics.diagnostics, item => item.code == "VALK_INVALID_CAST");
        Assert.Contains("string", diagnostic.message, StringComparison.Ordinal);
        Assert.Contains("i32", diagnostic.message, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildLir_F64ToI32Cast_ShouldEmitConversionOpcode()
    {
        var lir = build_lir(
            """
            namespace app;

            micro to_i32(value: f64) -> i32 {
                return value as i32;
            }

            [main]
            micro main() -> ExitCode {
                return ExitCode(to_i32(42.5));
            }
            """,
            "numeric_cast.v");
        var function = Assert.Single(lir.module.Functions, item => item.Name == "app.to_i32");

        Assert.Contains(function.Instructions, instruction => instruction.Opcode == NyarHeadCode.F64ToI32);
    }

    private static global::Nyar.Language.Valkyrie.Compiler.Lir.LirModule build_lir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("cast_semantics", "jvm-openjdk-linux-managed", fileName);
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

using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Assembler.Backends.Wasm;
using Nyar.Assembler;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;
using Std.Data.Binary.NyarIR.Data;
using Std.Data.Text.Valkyrie.AST;

namespace Valkyrie.Tests.CompilerTests;

public sealed class GeneratorLoweringTests
{
    [Fact]
    public void BuildLir_YieldStatements_ShouldLowerToPerformEffectInstructions()
    {
        var function = build_lir_function(
            """
            namespace app;

            [main]
            micro main() -> Generator<i32> {
                yield 1;
                yield break;
                yield return 2;
            }
            """,
            "generator_yield_lowering.v");

        Assert.Equal(4, function.instructions.Count(instruction => instruction.opcode == NyarHeadCode.perform_effect));
        Assert.Equal(
            ["Yielder::Yield", "Yielder::YieldBreak", "Yielder::Yield", "Yielder::YieldBreak"],
            get_string_constants(function));
    }

    [Fact]
    public void BuildLir_YieldThenLetThenYieldReturn_ShouldKeepStatementsAfterFirstYield()
    {
        var function = build_lir_function(
            """
            namespace app;

            [main]
            micro main() -> Generator<i32> {
                yield 1;
                let later = 2;
                yield return later;
            }
            """,
            "generator_yield_sequence.v");

        Assert.Contains(function.local_variables, local => local.name == "later");
        Assert.Equal(3, function.instructions.Count(instruction => instruction.opcode == NyarHeadCode.perform_effect));
        Assert.Equal(
            ["Yielder::Yield", "Yielder::Yield", "Yielder::YieldBreak"],
            get_string_constants(function));
    }

    [Fact]
    public void BuildLir_YieldBreak_ShouldStopLoweringFollowingStatements()
    {
        var function = build_lir_function(
            """
            namespace app;

            [main]
            micro main() -> Generator<i32> {
                yield break;
                let unreachable = 2;
                yield 3;
            }
            """,
            "generator_yield_break_terminates.v");

        Assert.DoesNotContain(function.local_variables, local => local.name == "unreachable");
        Assert.Equal(1, function.instructions.Count(instruction => instruction.opcode == NyarHeadCode.perform_effect));
        Assert.Equal(["Yielder::YieldBreak"], get_string_constants(function));
    }

    [Fact]
    public void BuildLir_YieldStatements_ShouldValidateForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("generator_lowering", "jvm-openjdk-linux-managed", "generator_yield_validate.v");
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main() -> Generator<i32> {
                yield 1;
                yield break;
                yield return 2;
            }
            """,
            "generator_yield_validate.v");

        var module = compiler.build_lir(mir, plan).module;

        Assert.True(new JvmBackend().validate(module, out var jvmDiagnostics),
            string.Join(Environment.NewLine, jvmDiagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.True(new ClrBackend().validate(module, out var clrDiagnostics),
            string.Join(Environment.NewLine, clrDiagnostics.Select(diagnostic => diagnostic.Message)));
        Assert.True(new WasmBackend().validate(module, out var wasmDiagnostics),
            string.Join(Environment.NewLine, wasmDiagnostics.Select(diagnostic => diagnostic.Message)));
    }

    private static global::Nyar.Language.Valkyrie.Compiler.Mir.MirModule build_mir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("generator_lowering", "jvm-openjdk-linux-managed", fileName);
        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var parseResult = compiler.parse_source(source, fileName);

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        return compiler.build_mir(hir, plan, targetProfile);
    }

    private static Nyar.Assembler.GenerateFunction build_lir_function(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("generator_lowering", "jvm-openjdk-linux-managed", fileName);
        var mir = build_mir(source, fileName);
        var module = compiler.build_lir(mir, plan).module;
        var exportedEntry = module.exports
            .FirstOrDefault(exportItem => exportItem.kind == Nyar.Assembler.GenerateExportKind.function);

        if (exportedEntry is not null &&
            exportedEntry.function_index >= 0 &&
            exportedEntry.function_index < module.functions.Count)
        {
            return module.functions[exportedEntry.function_index];
        }

        return module.functions.Single();
    }

    private static IReadOnlyList<string> get_string_constants(GenerateFunction function)
    {
        return function.instructions
            .Where(instruction => instruction.opcode == NyarHeadCode.@const)
            .SelectMany(instruction => instruction.operands)
            .OfType<GenerateOperand.Str>()
            .Select(operand => operand.value)
            .ToArray();
    }
}

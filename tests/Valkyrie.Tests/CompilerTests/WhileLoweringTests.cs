using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Assembler.Backends.Wasm;
using Std.Data.Binary.NyarIR.Data;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class WhileLoweringTests
{
    [Fact]
    public void BuildMir_While_ShouldLowerToRepeatWithCondition()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main(flag: bool) -> Unit {
                while (flag) {
                    let matched = 1;
                }
            }
            """,
            "while_lowering.v");

        var repeatNode = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<IKun.Repeat>()
            .Single();

        var conditionNodes = mir.graph.classes[mir.graph.union_find.find(repeatNode.count).value].nodes;
        var bodyNodes = mir.graph.classes[mir.graph.union_find.find(repeatNode.body).value].nodes;

        Assert.Contains(conditionNodes, node => node is IKun.Symbol { name: "flag" });
        Assert.Contains(bodyNodes, node => node is IKun.VarDecl { name: "matched" });
    }

    [Fact]
    public void BuildMir_Until_ShouldLowerToGuardedRepeat()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main(done: bool) -> Unit {
                until (done) {
                    let matched = 1;
                }
            }
            """,
            "until_lowering.v");

        var repeatNode = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<IKun.Repeat>()
            .Single();

        var conditionNodes = mir.graph.classes[mir.graph.union_find.find(repeatNode.count).value].nodes;
        var bodyNodes = mir.graph.classes[mir.graph.union_find.find(repeatNode.body).value].nodes;
        var allNodes = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .ToArray();

        Assert.Contains(conditionNodes, node => node is IKun.BooleanConstant { value: true });
        Assert.Contains(bodyNodes, node => node is IKun.Choice);
        Assert.Contains(allNodes, node => node is IKun.Break);
        Assert.Contains(allNodes, node => node is IKun.VarDecl { name: "matched" });
    }

    [Fact]
    public void BuildLir_While_ShouldLowerToLoopBranches()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("while_lowering", "jvm-openjdk-linux-managed", "while_lir_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(flag: bool) -> Unit {
                while (flag) {
                    let matched = 1;
                }
            }
            """,
            "while_lir_boundary.v");

        var function = compiler.build_lir(mir, plan).module.functions.Single();

        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.JumpIfFalse);
        Assert.Contains(function.instructions, instruction => instruction.opcode == NyarHeadCode.Jump);
        Assert.True(function.labels.Count >= 2);
    }

    [Fact]
    public void BuildLir_While_ShouldCompileForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("while_lowering", "jvm-openjdk-linux-managed", "while_backend_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(flag: bool) -> Unit {
                while (flag) {
                    let matched = 1;
                }
            }
            """,
            "while_backend_boundary.v");

        var module = compiler.build_lir(mir, plan).module;

        Assert.True(new JvmBackend().validate(module, out var jvmDiagnostics),
            string.Join(Environment.NewLine, jvmDiagnostics.Select(diagnostic => diagnostic.message)));
        Assert.True(new ClrBackend().validate(module, out var clrDiagnostics),
            string.Join(Environment.NewLine, clrDiagnostics.Select(diagnostic => diagnostic.message)));
        Assert.True(new WasmBackend().validate(module, out var wasmDiagnostics),
            string.Join(Environment.NewLine, wasmDiagnostics.Select(diagnostic => diagnostic.message)));

        var jvmOutput = new JvmBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        var clrOutput = new ClrBackend().compile(module, new Nyar.Assembler.CompilationOptions());
        var wasmOutput = new WasmBackend().compile(module, new Nyar.Assembler.CompilationOptions());

        Assert.Equal(".class", jvmOutput.file_extension);
        Assert.NotNull(jvmOutput.data);
        Assert.NotNull(clrOutput.data);
        Assert.Equal(".wasm", wasmOutput.file_extension);
        Assert.NotNull(wasmOutput.data);
    }

    [Fact]
    public void BuildLir_Until_WithBreak_ShouldUseRepeatEndAndSkipUnreachableStatements()
    {
        var function = build_lir_function(
            """
            namespace app;

            [main]
            micro main(done: bool) -> Unit {
                until (done) {
                    break;
                    let after_break = 1;
                }
            }
            """,
            "until_break_unreachable.v");

        Assert.Contains(get_jump_label_names(function), label => label.Contains("repeat_end", StringComparison.Ordinal));
        Assert.DoesNotContain(function.local_variables, local => local.name == "after_break");
    }

    private static global::Nyar.Language.Valkyrie.Compiler.Mir.MirModule build_mir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("while_lowering", "jvm-openjdk-linux-managed", fileName);
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
        var plan = new BuildPlan("while_lowering", "jvm-openjdk-linux-managed", fileName);
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

    private static IReadOnlyList<string> get_jump_label_names(Nyar.Assembler.GenerateFunction function)
    {
        return function.instructions
            .Where(instruction => instruction.opcode == NyarHeadCode.jump)
            .SelectMany(instruction => instruction.operands)
            .OfType<Nyar.Assembler.GenerateOperand.Label>()
            .Select(label => label.name)
            .ToArray();
    }
}

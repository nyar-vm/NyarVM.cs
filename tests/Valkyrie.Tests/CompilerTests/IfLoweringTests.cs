using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Assembler.Backends.Wasm;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class IfLoweringTests
{
    [Fact]
    public void BuildMir_IfElse_ShouldLowerToBooleanMatch()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main(flag: bool) -> Unit {
                if (flag) {
                    return
                }
                else {
                    return
                }
            }
            """,
            "if_else_lowering.v");

        var matchNode = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<IKun.Match>()
            .Single();

        Assert.Equal(2, matchNode.arms.Length);

        var trueArm = mir.graph.classes[mir.graph.union_find.find(matchNode.arms[0]).value].nodes
            .OfType<IKun.MatchArm>()
            .Single();
        var elseArm = mir.graph.classes[mir.graph.union_find.find(matchNode.arms[1]).value].nodes
            .OfType<IKun.MatchArm>()
            .Single();

        var truePattern = mir.graph.classes[mir.graph.union_find.find(trueArm.pattern).value].nodes
            .OfType<IKun.LiteralPattern>()
            .Single();
        var literalValue = mir.graph.classes[mir.graph.union_find.find(truePattern.value).value].nodes
            .OfType<IKun.BooleanConstant>()
            .Single();
        var elsePattern = mir.graph.classes[mir.graph.union_find.find(elseArm.pattern).value].nodes
            .OfType<IKun.WildcardPattern>()
            .Single();

        Assert.True(literalValue.value);
        Assert.NotNull(elsePattern);
    }

    [Fact]
    public void BuildMir_ElseIfChain_ShouldLowerToNestedMatches()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main(flag: bool, fallback: bool) -> Unit {
                if (flag) {
                    return
                }
                else if (fallback) {
                    return
                }
                else {
                    return
                }
            }
            """,
            "else_if_lowering.v");

        var matches = mir.graph.classes.Values
            .SelectMany(@class => @class.nodes)
            .OfType<IKun.Match>()
            .ToArray();

        Assert.True(matches.Length >= 2);
    }

    [Fact]
    public void BuildLir_IfElse_ShouldCompileForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("if_lowering", "jvm-openjdk-linux-managed", "if_backend_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(flag: bool) -> Unit {
                if (flag) {
                    let matched = 1;
                }
                else {
                    let fallback = 0;
                }
            }
            """,
            "if_backend_boundary.v");

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

    private static global::Nyar.Language.Valkyrie.Compiler.Mir.MirModule build_mir(string source, string fileName)
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("if_lowering", "jvm-openjdk-linux-managed", fileName);
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
}

using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Assembler.Backends.Wasm;
using Nyar.IR.Intent;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class IsLoweringTests
{
    [Fact]
    public void BuildMir_IsOnPrimitiveParameter_ShouldLowerToBooleanConstant()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main(flag: bool) -> Unit {
                let matched = flag is bool;
            }
            """,
            "is_primitive_true.v");

        Assert.Contains(
            mir.graph.classes.Values.SelectMany(@class => @class.nodes).OfType<IKun.BooleanConstant>(),
            constant => constant.value);
    }

    [Fact]
    public void BuildMir_IsOnPrimitiveMismatch_ShouldLowerToBooleanConstantFalse()
    {
        var mir = build_mir(
            """
            namespace app;

            [main]
            micro main(flag: bool) -> Unit {
                let matched = flag is string;
            }
            """,
            "is_primitive_false.v");

        Assert.Contains(
            mir.graph.classes.Values.SelectMany(@class => @class.nodes).OfType<IKun.BooleanConstant>(),
            constant => !constant.value);
    }

    [Fact]
    public void BuildLir_IsOnPrimitiveType_ShouldCompileForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("is_lowering", "jvm-openjdk-linux-managed", "is_backend_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            micro main(flag: bool) -> Unit {
                let matched = flag is bool;
                let fallback = flag is string;
            }
            """,
            "is_backend_boundary.v");

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
        var plan = new BuildPlan("is_lowering", "jvm-openjdk-linux-managed", fileName);
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

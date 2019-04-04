using Nyar.Assembler.Backends.Clr;
using Nyar.Assembler.Backends.Jvm;
using Nyar.Assembler.Backends.Wasm;
using Std.Data.Binary.NyarIR.Data;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class PowerLoweringTests
{
    [Fact]
    public void BuildLir_WithLibraryImplementedPowerOperator_ShouldCompileForJvmClrAndWasm()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("power_lowering", "jvm-openjdk-linux-managed", "power_backend_boundary.v");
        var mir = build_mir(
            """
            namespace app;

            imply i32 {
                infix `^`(self, rhs: Self): Self {
                    __i32_pow(self, rhs)
                }
            }

            private micro __i32_pow(lhs: i32, rhs: i32): i32 {
                if rhs < 0 {
                    return 0
                }

                let mut base: i32 = lhs
                let mut exp: i32 = rhs
                let mut acc: i32 = 1

                while exp > 0 {
                    if exp % 2 == 1 {
                        acc = acc * base
                    }

                    base = base * base
                    exp = exp / 2
                }

                return acc
            }

            [main]
            micro main() -> i32 {
                2 ^ 5
            }
            """,
            "power_backend_boundary.v");

        var module = compiler.build_lir(mir, plan).module;
        var powFunction = Assert.Single(module.functions,
            function => function.name.EndsWith("__i32_pow", StringComparison.Ordinal));

        Assert.Contains(powFunction.instructions, instruction => instruction.opcode == NyarHeadCode.JumpIfFalse);
        Assert.Contains(powFunction.instructions, instruction => instruction.opcode == NyarHeadCode.Jump);
        Assert.Contains(powFunction.instructions, instruction => instruction.opcode == NyarHeadCode.I32Mul);
        Assert.Contains(powFunction.instructions, instruction => instruction.opcode == NyarHeadCode.I32DivS);
        Assert.Contains(powFunction.instructions, instruction => instruction.opcode == NyarHeadCode.I32RemS);

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
        var plan = new BuildPlan("power_lowering", "jvm-openjdk-linux-managed", fileName);
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

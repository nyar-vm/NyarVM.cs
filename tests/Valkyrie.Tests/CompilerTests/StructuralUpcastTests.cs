using Std.Data.Binary.NyarIR.Data;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class StructuralUpcastTests
{
    [Fact]
    public void Analyze_StructuralTraitUpcast_ShouldAllowImplicitAndExplicitUpcast()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("structural_upcast", "jvm-openjdk-linux-managed", "structural_upcast.v");
        var parseResult = compiler.parse_source(
            """
            namespace app;

            trait Speaker {
                micro show(self) -> string
            }

            class Dog {
                micro show(self) -> string {
                    return "Dog"
                }
            }

            micro accept(value: Speaker) -> Unit {
                print("Structural Upcast OK")
            }

            [main]
            micro main() -> ExitCode {
                let dog = Dog {}
                accept(dog)
                let speaker = dog as Speaker
                accept(speaker)
                return ExitCode(0 as i32)
            }
            """,
            "structural_upcast.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);

        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));
    }

    [Fact]
    public void BuildLir_StructuralTraitUpcast_ShouldNotEmitNumericCastOpcode()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("structural_upcast", "jvm-openjdk-linux-managed", "structural_upcast.v");
        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var parseResult = compiler.parse_source(
            """
            namespace app;

            trait Speaker {
                micro show(self) -> string
            }

            class Dog {
                micro show(self) -> string {
                    return "Dog"
                }
            }

            [main]
            micro main() -> ExitCode {
                let dog = Dog {}
                let speaker = dog as Speaker
                return ExitCode(0 as i32)
            }
            """,
            "structural_upcast.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);
        var main = Assert.Single(lir.module.functions, function => function.name == "app.main");

        Assert.DoesNotContain(main.instructions, instruction =>
            instruction.opcode is NyarHeadCode.I32ExtendI64S
                or NyarHeadCode.I64TruncI32S
                or NyarHeadCode.I32ToF32S
                or NyarHeadCode.I32ToF64S
                or NyarHeadCode.I64ToF64
                or NyarHeadCode.F64ToI32
                or NyarHeadCode.F64ToI64);
    }

    [Fact]
    public void CompileToTarget_Jvm_WithStructuralTraitUpcast_ShouldProduceArtifacts()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var plan = new BuildPlan("structural_upcast", "jvm-openjdk-linux-managed", "structural_upcast.v");
        var artifacts = compiler.compile_to_target(
            """
            namespace app;

            trait Speaker {
                micro show(self) -> string
            }

            class Dog {
                micro show(self) -> string {
                    return "Dog"
                }
            }

            micro accept(value: Speaker) -> Unit {
                print("Structural Upcast OK")
            }

            [main]
            micro main() -> ExitCode {
                let dog = Dog {}
                accept(dog)
                let speaker = dog as Speaker
                accept(speaker)
                return ExitCode(0 as i32)
            }
            """,
            plan);

        Assert.Equal("structural_upcast.class", artifacts.primary_artifact.name);
        Assert.Contains(artifacts.sidecar_artifacts, artifact => artifact.name == "structural_upcast.jar");
    }
}

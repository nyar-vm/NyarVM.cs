using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Lir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Mir;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;

namespace Valkyrie.Tests.CompilerTests;

public sealed class WitnessAotBoundaryTests
{
    [Fact]
    public void BuildHir_WithTraitDefaultMethod_ShouldExcludeTraitDefaultMethodFromAotCallables()
    {
        var result = build_pipeline_modules();
        var allCallables = result.hir.enumerate_callables();
        var aotCallables = result.hir.enumerate_aot_callables();

        Assert.Contains(result.trait_default_method, allCallables);
        Assert.DoesNotContain(result.trait_default_method, aotCallables);
        Assert.Contains(result.imply_method, aotCallables);
    }

    [Fact]
    public void BuildMirAndLir_WithWitnessMetadataOnly_ShouldPreserveMetadataWithoutWitnessDispatch()
    {
        var result = build_pipeline_modules();

        Assert.Single(result.mir.witness_bindings);
        Assert.False(result.mir.has_witness_dispatch);

        var witnessEntry = Assert.Single(result.lir.module.witness_entries);
        Assert.True(result.lir.module.has_witness_entries);
        Assert.False(result.lir.module.has_witness_dispatch);
        Assert.True(witnessEntry.method_id > 0);
        Assert.True(witnessEntry.type_id > 0);
        Assert.True(witnessEntry.interface_id > 0);
        Assert.Equal(witnessEntry.slot_index, witnessEntry.interface_method_index);
        Assert.DoesNotContain(result.lir.module.functions,
            function => function.name == result.trait_default_method.name);
    }

    private static PipelineModules build_pipeline_modules()
    {
        var compiler = new ValkyrieCompiler();
        var plan = create_jvm_build_plan();
        var targetProfile = create_jvm_target_profile();
        var source = """
                     namespace app;

                     trait Map {
                         micro set(mut self, value: i32) -> Unit

                         micro insert(mut self, value: i32) -> Unit {
                             self.set(value)
                         }
                     }

                     structure Buffer {
                     }

                     imply Buffer: Map {
                         micro set(mut self, value: i32) -> Unit {
                         }
                     }

                     [main]
                     micro main() -> Unit {
                         print("trait on jvm")
                     }
                     """;

        var parseResult = compiler.parse_source(source, "trait.v");
        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);

        var traitDefaultMethod = hir.traits
            .Single()
            .methods
            .Single(method => method.member_name == "insert");
        var implyMethod = hir.implys
            .Single()
            .methods
            .Single(method => method.member_name == "set");

        return new PipelineModules(hir, mir, lir, traitDefaultMethod, implyMethod);
    }

    private static BuildPlan create_jvm_build_plan()
    {
        return new BuildPlan("trait_boundary", "jvm-openjdk-linux-managed", "trait.v");
    }

    private static TargetProfile create_jvm_target_profile()
    {
        return new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
    }

    private sealed record PipelineModules(
        HirModule hir,
        MirModule mir,
        LirModule lir,
        HirMethod trait_default_method,
        HirMethod imply_method);
}

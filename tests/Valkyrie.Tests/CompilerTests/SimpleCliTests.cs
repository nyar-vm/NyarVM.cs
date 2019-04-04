using Nyar.Assembler;
using Nyar.Dialect.Core.Nodes;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Lir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;
using Nyar.Language.Valkyrie.Compiler.Targets;
using Nyar.Types.Targets;
using Std.Data.Text.Valkyrie.AST;

namespace Valkyrie.Tests.CompilerTests;

public sealed class SimpleCliTests
{
    [Fact]
    public void HirMain_MainAttribute_ShouldBeRecognizedAsLogicalEntry()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var source = """
            namespace simple_cli;

            [main]
            micro main() -> Unit {
            }
            """;
        var plan = new BuildPlan("simple_cli", "jvm-openjdk-linux-managed", "simple_cli.v");
        var parseResult = compiler.parse_source(source, "simple_cli.v");

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors,
            string.Join(Environment.NewLine, compiler.diagnostics.messages.Select(d => d.message)));

        var stagedAst = (ProgramRoot)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(d => d.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        Assert.NotEmpty(hir.functions);

        var mainFunc = hir.functions.FirstOrDefault(f => f.name.EndsWith(".main"));
        Assert.NotNull(mainFunc);
        Assert.True(mainFunc.is_logical_entry, "main should be recognized as logical entry");
    }

    [Fact]
    public void MirMain_MainFunction_ShouldBeExported()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var source = """
            namespace simple_cli;

            [main]
            micro main() -> Unit {
            }
            """;
        var plan = new BuildPlan("simple_cli", "jvm-openjdk-linux-managed", "simple_cli.v");
        var parseResult = compiler.parse_source(source, "simple_cli.v");
        Assert.NotNull(parseResult.value);

        var stagedAst = (ProgramRoot)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(d => d.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var mir = compiler.build_mir(hir, plan, targetProfile);

        Assert.True(mir.root.HasValue, "MIR root should exist");
        var mainExported = mir.is_exported_function("simple_cli.main");
        Assert.True(mainExported, "main should be exported in MIR");
    }

    [Fact]
    public void LirMain_MainFunction_ShouldProduceInstructions()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var source = """
            namespace simple_cli;

            [main]
            micro main() -> Unit {
            }
            """;
        var plan = new BuildPlan("simple_cli", "jvm-openjdk-linux-managed", "simple_cli.v");
        var parseResult = compiler.parse_source(source, "simple_cli.v");
        Assert.NotNull(parseResult.value);

        var stagedAst = (ProgramRoot)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(d => d.message)));

        var hir = compiler.build_hir(stagedAst, semantics, plan);
        var targetProfile = new CanonicalTargetRegistry().resolve("jvm-openjdk-linux-managed");
        var mir = compiler.build_mir(hir, plan, targetProfile);
        var lir = compiler.build_lir(mir, plan);

        Assert.NotEmpty(lir.module.functions);
        var mainFunc = lir.module.functions.FirstOrDefault(f => f.name == "simple_cli.main");
        Assert.NotNull(mainFunc);
    }

    [Fact]
    public void FullPipeline_SimpleCli_ShouldProduceArtifacts()
    {
        var compiler = new global::Nyar.Language.Valkyrie.Compiler.ValkyrieCompiler();
        var source = """
            namespace simple_cli;

            [main]
            micro main() -> Unit {
            }
            """;
        var plan = new BuildPlan("simple_cli", "jvm-openjdk-linux-managed", "simple_cli.v");
        var result = compiler.compile_to_target(source, plan);
        Assert.NotEmpty(result.primary_artifact.content);
    }
}


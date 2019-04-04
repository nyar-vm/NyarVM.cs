using Nyar.Language.Valkyrie.Compiler;
using Nyar.Language.Valkyrie.Compiler.Hir;
using Nyar.Language.Valkyrie.Compiler.Meta;
using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Valkyrie.Tests.CompilerTests;

public sealed class IntrinsicAnnotationTests
{
    [Fact]
    public void BuildHir_WithIntrinsicAnnotation_ShouldBindIntrinsicSemantics()
    {
        const string source = """
                              [intrinsic("i32.add")]
                              micro __i32_add(lhs: i32, rhs: i32): i32 { }
                              """;

        var module = build_hir(source, "intrinsic_annotation.v");
        var function = Assert.Single(module.functions);

        Assert.Equal("i32.add", function.semantics.intrinsic?.name);
        Assert.DoesNotContain(function.surface_attributes,
            attribute => string.Equals(attribute.name, "intrinsic", StringComparison.Ordinal));
    }

    [Fact]
    public void BuildHir_WithIntrinsicAnnotation_ShouldStripSurfaceIntrinsicAttribute()
    {
        const string source = """
                              [intrinsic("i32.add")]
                              micro __i32_add(lhs: i32, rhs: i32): i32 { }
                              """;

        var module = build_hir(source, "intrinsic_surface_strip.v");
        var function = Assert.Single(module.functions);

        Assert.Equal("i32.add", function.semantics.intrinsic?.name);
        Assert.DoesNotContain(function.surface_attributes,
            attribute => string.Equals(attribute.name, "intrinsic", StringComparison.Ordinal));
    }

    private static HirModule build_hir(string source, string fileName)
    {
        var compiler = new ValkyrieCompiler();
        var plan = new BuildPlan("intrinsic_annotation", "jvm-openjdk-linux-managed", fileName);
        var parseResult = compiler.parse_source(source, fileName);

        Assert.NotNull(parseResult.value);
        Assert.False(compiler.diagnostics.has_errors);

        var stagedAst = (CompilationUnit)new MetaStager().stage(parseResult.value!, plan.canonical_triple);
        var semantics = compiler.analyze(stagedAst, plan);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.message)));
        return compiler.build_hir(stagedAst, semantics, plan);
    }
}


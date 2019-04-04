using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Semantic;
using Std.Data.Text.Parsing;
using Std.Data.Text.Valkyrie.Semantic;

namespace Valkyrie.Tests.SemanticTests;

public sealed class TupleSemanticTests
{
    [Fact]
    public void Analyze_NamedTupleAnnotation_ShouldPreserveLabels()
    {
        var semantics = analyze(
            """
            micro pick(pair: (ordinal: usize, value: i32)) -> i32 {
                return pair.2
            }
            """);

        var pick = Assert.IsType<FunctionType>(semantics.resolve_symbol("pick")?.type);
        var pair = Assert.IsType<NamedType>(pick.parameter_types[0]);

        Assert.Equal("tuple", pair.kind_tag);
        Assert.Equal("(ordinal: usize, value: i32)", pair.name);
        Assert.Equal(2, pair.type_arguments.Count);
        Assert.Equal("ordinal", Assert.Single(pair.members, member => member.name == "ordinal").name);
        Assert.Equal("value", Assert.Single(pair.members, member => member.name == "value").name);
    }

    [Fact]
    public void Analyze_ArrayOfTupleLiteralWithAnnotation_ShouldUseContextualTypeInference()
    {
        var semantics = analyze(
            """
            micro build_pairs() -> [(i32, i32)] {
                let x: [(i32, i32)] = [(0, 1)]
                return x
            }
            """);

        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(diagnostic => diagnostic.ToString())));
    }

    private static SemanticModel analyze(string source)
    {
        var analyzer = new SourceAnalyzer();
        var ast = assert_parse_success(analyzer.parse(source, "tuple_semantic.v"));
        var bridge = new ValkyrieSemanticBridge();
        return bridge.build_semantic_model(new TypeCheckResult([]), ast, "tuple_semantic.v");
    }

    private static CompilationUnit assert_parse_success(ParseResult<CompilationUnit> result)
    {
        Assert.True(result.success, string.Join(Environment.NewLine, result.diagnostics.Select(d => d.message)));
        return Assert.IsType<CompilationUnit>(result.value);
    }
}

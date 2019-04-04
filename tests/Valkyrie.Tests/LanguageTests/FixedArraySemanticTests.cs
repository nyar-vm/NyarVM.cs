using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.TypeSystem;
using Nyar.Language.Valkyrie.Semantic;
using Std.Data.Text.Valkyrie.Semantic;

namespace Valkyrie.Tests.LanguageTests;

public sealed class FixedArraySemanticTests
{
    [Fact]
    public void FixedArrayType_ShouldBeValueType()
    {
        var fixedArray = ValkyrieType.fixed_array(ValkyrieType.i32, 3);

        Assert.True(fixedArray.is_value_type);
        Assert.False(fixedArray.is_reference_type);
    }

    [Fact]
    public void Analyze_FixedArrayAnnotation_ShouldBridgeAsNamedType()
    {
        var semantics = Analyze("""
                                micro head(input: [i32; 3]) -> i32 {
                                    return input[0]
                                }
                                """);

        var head = Assert.IsType<FunctionType>(semantics.resolve_symbol("head")?.type);
        var input = Assert.IsType<NamedType>(head.parameter_types[0]);

        Assert.Equal("FixedArray", input.name);
        Assert.Equal(2, input.type_arguments.Count);
    }

    [Fact]
    public void Analyze_NamedTupleAnnotation_ShouldPreserveLabels()
    {
        var semantics = Analyze("""
                                micro pick(pair: (ordinal: usize, value: i32)) -> i32 {
                                    pair.2
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

    private static SemanticModel Analyze(string source)
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.parse(source, "fixed_array_semantic.v").value!;
        var bridge = new ValkyrieSemanticBridge();
        return bridge.build_semantic_model(new TypeCheckResult([]), ast, "fixed_array_semantic.v");
    }
}

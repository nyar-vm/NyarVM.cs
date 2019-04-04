using Nyar.Analyzer.Semantic;
using Nyar.Language.Valkyrie.Semantic;
using Std.Data.Text.Parsing;
using Std.Data.Text.Valkyrie.Semantic;

namespace Valkyrie.Tests.SemanticTests;

public sealed class MainReturnInferenceTests
{
    [Fact]
    public void Analyze_MainWithoutExplicitReturnType_ShouldInferExitCode()
    {
        var analyzer = new SourceAnalyzer();
        var source = """
                     [main]
                     micro hello_world() {
                         print("Hello World!")
                         ExitCode(0 as i32)
                     }
                     """;
        var ast = assert_parse_success(analyzer.parse(source, "main.v"));
        var bridge = new ValkyrieSemanticBridge();
        var function = Assert.IsType<DeclareMicro>(ast.declarations[0]);
        var body = Assert.IsType<FunctionBody>(function.body);
        var inferred = new ValkyrieTypeInference([function], bridge).infer_function_return_type(function);

        var semantics = bridge.build_semantic_model(new TypeCheckResult([]), ast, "main.v");

        Assert.Equal(2, body.statements.Count);
        Assert.IsAssignableFrom<TermNode>(body.statements[^1]);
        Assert.Equal("ExitCode", inferred.name);
        Assert.False(semantics.has_errors,
            string.Join(Environment.NewLine, semantics.diagnostics.Select(d => d.ToString())));
    }

    [Fact]
    public void Analyze_NamespacedNestedCall_ShouldInferIntegerReturnType()
    {
        var analyzer = new SourceAnalyzer();
        var source = """
                     namespace hello_world;

                     micro add_one(x: isize) {
                         let one = 1;
                         return x + one;
                     }

                     micro add_two(x: isize) {
                         return add_one(add_one(x));
                     }
                     """;
        var ast = assert_parse_success(analyzer.parse(source, "nested_return.v"));
        var bridge = new ValkyrieSemanticBridge();
        var functions = ast.declarations.OfType<DeclareMicro>().ToArray();
        var addTwo = Assert.Single(functions, function => function.name?.name == "add_two");
        var inferred = new ValkyrieTypeInference(functions, bridge).infer_function_return_type(addTwo);

        Assert.Equal("isize", inferred.name);
    }

    [Fact]
    public void Analyze_TupleLiteralReturn_ShouldInferTupleType()
    {
        var analyzer = new SourceAnalyzer();
        var source = """
                     micro build_pair() {
                         return (1 as usize, 2 as i32)
                     }
                     """;
        var ast = assert_parse_success(analyzer.parse(source, "tuple_return.v"));
        var bridge = new ValkyrieSemanticBridge();
        var function = Assert.IsType<DeclareMicro>(ast.declarations[0]);
        var inferred = Assert.IsType<NamedType>(new ValkyrieTypeInference([function], bridge)
            .infer_function_return_type(function));

        Assert.Equal("tuple", inferred.kind_tag);
        Assert.Equal("(usize, i32)", inferred.name);
        Assert.Equal(2, inferred.type_arguments.Count);
    }

    [Fact]
    public void Analyze_IntegerLiteralSuffix_ShouldInferExplicitNumericTypes()
    {
        var analyzer = new SourceAnalyzer();
        var source = """
                     micro build_pair() {
                         return (1_usize, 2_i32)
                     }
                     """;
        var ast = assert_parse_success(analyzer.parse(source, "tuple_suffix_return.v"));
        var bridge = new ValkyrieSemanticBridge();
        var function = Assert.IsType<DeclareMicro>(ast.declarations[0]);
        var inferred = Assert.IsType<NamedType>(new ValkyrieTypeInference([function], bridge)
            .infer_function_return_type(function));

        Assert.Equal("tuple", inferred.kind_tag);
        Assert.Equal("(usize, i32)", inferred.name);
        Assert.Equal(2, inferred.type_arguments.Count);
    }

    [Fact]
    public void Analyze_UnconstrainedIntegerLiteral_ShouldFallbackToI32()
    {
        var analyzer = new SourceAnalyzer();
        var source = """
                     micro value() {
                         return 42
                     }
                     """;
        var ast = assert_parse_success(analyzer.parse(source, "fallback_integer_return.v"));
        var bridge = new ValkyrieSemanticBridge();
        var function = Assert.IsType<DeclareMicro>(ast.declarations[0]);
        var inferred = new ValkyrieTypeInference([function], bridge).infer_function_return_type(function);

        Assert.Equal("i32", inferred.name);
    }

    [Fact]
    public void Analyze_UnconstrainedFloatLiteral_ShouldFallbackToF32()
    {
        var analyzer = new SourceAnalyzer();
        var source = """
                     micro value() {
                         return 3.14
                     }
                     """;
        var ast = assert_parse_success(analyzer.parse(source, "fallback_float_return.v"));
        var bridge = new ValkyrieSemanticBridge();
        var function = Assert.IsType<DeclareMicro>(ast.declarations[0]);
        var inferred = new ValkyrieTypeInference([function], bridge).infer_function_return_type(function);

        Assert.Equal("f32", inferred.name);
    }

    [Fact]
    public void Analyze_FloatInitializerAssignedToI32_ShouldReportTypeMismatch()
    {
        var analyzer = new SourceAnalyzer();
        var source = """
                     micro main() -> i32 {
                         let x: i32 = 3.14
                         return x
                     }
                     """;
        var ast = assert_parse_success(analyzer.parse(source, "float_to_int_mismatch.v"));
        var bridge = new ValkyrieSemanticBridge();
        var semantics = bridge.build_semantic_model(new TypeCheckResult([]), ast, "float_to_int_mismatch.v");

        Assert.True(semantics.has_errors);
        Assert.Contains(semantics.diagnostics, diagnostic => diagnostic.code == "VALK_LET_INITIALIZER_TYPE");
    }

    [Fact]
    public void Analyze_LocalFromNamedTupleCall_ShouldInferOrdinalMemberType()
    {
        var analyzer = new SourceAnalyzer();
        var source = """
                     micro make_pair() -> (ordinal: usize, value: i32) {
                         return (1 as usize, 2 as i32)
                     }

                     micro pick_value() {
                         let pair = make_pair()
                         return pair.2
                     }
                     """;
        var ast = assert_parse_success(analyzer.parse(source, "tuple_member_return.v"));
        var bridge = new ValkyrieSemanticBridge();
        var functions = ast.declarations.OfType<DeclareMicro>().ToArray();
        var pickValue = Assert.Single(functions, function => function.name?.name == "pick_value");
        var inferred = new ValkyrieTypeInference(functions, bridge).infer_function_return_type(pickValue);

        Assert.Equal("i32", inferred.name);
    }

    private static CompilationUnit assert_parse_success(ParseResult<CompilationUnit> result)
    {
        Assert.True(result.success, string.Join(Environment.NewLine, result.diagnostics.Select(d => d.message)));
        return Assert.IsType<CompilationUnit>(result.value);
    }
}

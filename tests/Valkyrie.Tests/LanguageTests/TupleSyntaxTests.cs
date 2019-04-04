using Std.Data.Text.Parsing;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Semantic;

namespace Valkyrie.Tests.LanguageTests;

public sealed class TupleSyntaxTests
{
    [Fact]
    public void Parse_NamedTupleType_BuildsTupleTypeNode()
    {
        var analyzer = new SourceAnalyzer();
        var ast = assert_parse_success(analyzer.parse(
            """
            micro pick(pair: (ordinal: usize, value: i32)) -> i32 {
                return pair.2
            }
            """,
            "named_tuple_type_term.v"));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.declarations));
        var parameter = Assert.Single(Assert.Single(function.parameters).items);
        var tupleType = Assert.IsType<TypeLiteralTupleNode>(parameter.bound_type);
        var ret = Assert.IsType<ReturnStatement>(Assert.Single(function.body!.statements));
        var access = Assert.IsType<TermDotExpression>(ret.value);

        Assert.Equal("2", access.callee.full_name);
        Assert.Collection(tupleType.elements,
            element =>
            {
                Assert.Equal("ordinal", element.label?.name);
                var type = Assert.IsType<TypeLiteralNamePathNode>(element.type);
                Assert.Equal("usize", type.path.full_name);
            },
            element =>
            {
                Assert.Equal("value", element.label?.name);
                var type = Assert.IsType<TypeLiteralNamePathNode>(element.type);
                Assert.Equal("i32", type.path.full_name);
            });
    }

    [Fact]
    public void Parse_TupleOrdinalAssignment_UsesTermDotExpression()
    {
        var analyzer = new SourceAnalyzer();
        var ast = assert_parse_success(analyzer.parse(
            """
            micro main(pair: (i32, i32)) {
                pair.2 = 42
            }
            """,
            "tuple_ordinal_assign.v"));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.declarations));
        var assignStmt = Assert.IsType<AssignmentStatement>(Assert.Single(function.body!.statements));
        var left = Assert.IsType<TermDotExpression>(assignStmt.target);
        var right = Assert.IsType<TermLiteralNumberNode>(assignStmt.value);

        Assert.Equal(TermBinaryOperator.assign, assignStmt.@operator);
        Assert.Equal("2", left.callee.full_name);
        Assert.Equal("42", right.value);
    }

    [Fact]
    public void Parse_MatchTuplePattern_BuildsTuplePatternNode()
    {
        var analyzer = new SourceAnalyzer();
        var ast = assert_parse_success(analyzer.parse(
            """
            micro main(value: (i32, i32)) -> Unit {
                match value {
                    case (left, right):
                        return
                }
            }
            """,
            "tuple_match_pattern_term.v"));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.declarations));
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.body!.statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.arms));
        var tuplePattern = Assert.IsType<PatternLiteralTupleNode>(caseArm.pattern);

        Assert.Null(tuplePattern.path);
        Assert.Collection(tuplePattern.elements,
            element => Assert.Equal("left", Assert.IsType<PatternLiteralVariableNode>(element).name),
            element => Assert.Equal("right", Assert.IsType<PatternLiteralVariableNode>(element).name));
    }

    private static CompilationUnit assert_parse_success(ParseResult<CompilationUnit> result)
    {
        Assert.True(result.success, string.Join(Environment.NewLine, result.diagnostics.Select(d => d.message)));
        return Assert.IsType<CompilationUnit>(result.value);
    }
}

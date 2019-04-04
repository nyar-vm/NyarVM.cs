using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Semantic;

namespace Valkyrie.Tests.LanguageTests;

public sealed class TermParserTests
{
    [Fact]
    public void Parse_PowerOperator_UsesDedicatedPowerEnum()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> i32 {
                2 ^ 3
            }
            """,
            "power_term.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var binary = Assert.IsType<TermBinaryExpression>(Assert.Single(function.Body!.Statements));

        Assert.Equal(TermBinaryOperator.Power, binary.Operator);
    }

    [Fact]
    public void Parse_PowerOperator_IsRightAssociativeAndHigherThanMultiplication()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> i32 {
                2 * 3 ^ 4 ^ 5
            }
            """,
            "power_precedence.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var multiplication = Assert.IsType<TermBinaryExpression>(Assert.Single(function.Body!.Statements));
        var firstPower = Assert.IsType<TermBinaryExpression>(multiplication.Right);
        var nestedPower = Assert.IsType<TermBinaryExpression>(firstPower.Right);

        Assert.Equal(TermBinaryOperator.Multiplication, multiplication.Operator);
        Assert.Equal(TermBinaryOperator.Power, firstPower.Operator);
        Assert.Equal(TermBinaryOperator.Power, nestedPower.Operator);
    }

    [Fact]
    public void Parse_DoubleQuotedTextLiteral_PreservesLiteralTextKind()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> utf8 {
                "hello"
            }
            """,
            "literal_text_term.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var literal = Assert.IsType<TermLiteralTextNode>(Assert.Single(function.Body!.Statements));

        Assert.Equal("hello", literal.value);
        Assert.Equal(TextLiteralKind.literal_text, literal.literal_kind);
    }

    [Fact]
    public void Parse_SingleQuotedTextLiteral_PreservesLiteralCharKind()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> char {
                'x'
            }
            """,
            "literal_char_term.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var literal = Assert.IsType<TermLiteralTextNode>(Assert.Single(function.Body!.Statements));

        Assert.Equal("x", literal.value);
        Assert.Equal(TextLiteralKind.literal_char, literal.literal_kind);
    }

    [Fact]
    public void Parse_IntegerLiteralSuffix_PreservesRawLiteralText()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> i32 {
                1_000_i32
            }
            """,
            "literal_integer_suffix_term.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var literal = Assert.IsType<TermLiteralNumberNode>(Assert.Single(function.Body!.Statements));

        Assert.Equal("1_000_i32", literal.value);
    }

    [Fact]
    public void Parse_TupleLiteral_BuildsTupleLiteralNode()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> i32 {
                (1, 2)
            }
            """,
            "tuple_literal_term.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var tuple = Assert.IsType<TermLiteralTupleNode>(Assert.Single(function.Body!.Statements));

        Assert.Collection(tuple.elements,
            element => Assert.IsType<TermLiteralNumberNode>(element),
            element => Assert.IsType<TermLiteralNumberNode>(element));
    }

    [Fact]
    public void Parse_NamedTupleType_BuildsTupleTypeNode()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro pick(pair: (ordinal: usize, value: i32)) -> i32 {
                pair.2
            }
            """,
            "named_tuple_type_term.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var parameter = Assert.Single(Assert.Single(function.parameters).items);
        var tupleType = Assert.IsType<TypeLiteralTupleNode>(parameter.bound_type);
        var access = Assert.IsType<TermDotExpression>(Assert.Single(function.Body!.Statements));
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
    public void Parse_MatchTuplePattern_BuildsTuplePatternNode()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main(value: (i32, i32)) -> Unit {
                match value {
                    case (left, right):
                        return
                }
            }
            """,
            "tuple_match_pattern_term.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));
        var tuplePattern = Assert.IsType<PatternLiteralTupleNode>(caseArm.Pattern);

        Assert.Null(tuplePattern.Path);
        Assert.Collection(tuplePattern.elements,
            element => Assert.Equal("left", Assert.IsType<PatternLiteralVariableNode>(element).Name),
            element => Assert.Equal("right", Assert.IsType<PatternLiteralVariableNode>(element).Name));
    }
}


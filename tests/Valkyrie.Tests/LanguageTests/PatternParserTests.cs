using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.Semantic;

namespace Valkyrie.Tests.LanguageTests;

public sealed class PatternParserTests
{
    [Fact]
    public void Parse_MatchWithBareTuplePattern_BuildsTuplePatternNode()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> Unit {
                match value {
                    case (left, right):
                        return
                }
            }
            """,
            "pattern_tuple.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));
        var tuplePattern = Assert.IsType<PatternLiteralTupleNode>(caseArm.Pattern);

        Assert.Null(tuplePattern.Path);
        Assert.Collection(tuplePattern.elements,
            element => Assert.Equal("left", Assert.IsType<PatternLiteralVariableNode>(element).Name),
            element => Assert.Equal("right", Assert.IsType<PatternLiteralVariableNode>(element).Name));
    }

    [Fact]
    public void Parse_MatchWithNumberPattern_PreservesNumericLiteralText()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> Unit {
                match value {
                    case 42:
                        return
                }
            }
            """,
            "pattern_number.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));
        var numberPattern = Assert.IsType<PatternLiteralNumberNode>(caseArm.Pattern);

        Assert.Equal("42", numberPattern.Value);
    }

    [Fact]
    public void Parse_MatchWithBooleanPattern_PreservesBooleanLiteralValue()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> Unit {
                match value {
                    case true:
                        return
                }
            }
            """,
            "pattern_boolean.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));
        var booleanPattern = Assert.IsType<PatternLiteralBooleanNode>(caseArm.Pattern);

        Assert.True(booleanPattern.Value);
    }

    [Fact]
    public void Parse_MatchWithNullPattern_PreservesNullLiteralNode()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> Unit {
                match value {
                    case null:
                        return
                }
            }
            """,
            "pattern_null.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));

        _ = Assert.IsType<PatternLiteralNullNode>(caseArm.Pattern);
    }

    [Fact]
    public void Parse_MatchWithObjectPattern_PreservesFieldNamesAndNestedPatterns()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> Unit {
                match value {
                    case Some { value: inner, flag }:
                        return
                }
            }
            """,
            "pattern_object.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));
        var objectPattern = Assert.IsType<PatternLiteralObjectNode>(caseArm.Pattern);
        var objectPath = Assert.IsType<QualifiedPathNode>(objectPattern.Path);

        Assert.Equal("Some", objectPath.FullName);
        Assert.Collection(objectPattern.fields,
            field =>
            {
                Assert.Equal("value", field.Name);
                Assert.Equal("inner", Assert.IsType<PatternLiteralVariableNode>(field.Pattern).Name);
            },
            field =>
            {
                Assert.Equal("flag", field.Name);
                Assert.Equal("flag", Assert.IsType<PatternLiteralVariableNode>(field.Pattern).Name);
            });
    }

    [Fact]
    public void Parse_MatchWithDoubleQuotedTextPattern_PreservesLiteralTextKind()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> Unit {
                match value {
                    case "hello":
                        return
                }
            }
            """,
            "pattern_text_literal.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));
        var textPattern = Assert.IsType<PatternLiteralTextNode>(caseArm.Pattern);

        Assert.Equal("hello", textPattern.value);
        Assert.Equal(TextLiteralKind.literal_text, textPattern.literal_kind);
    }

    [Fact]
    public void Parse_MatchWithSingleQuotedTextPattern_PreservesLiteralCharKind()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() -> Unit {
                match value {
                    case 'x':
                        return
                }
            }
            """,
            "pattern_char_literal.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var matchStatement = Assert.IsType<MatchStatementNode>(Assert.Single(function.Body!.Statements));
        var caseArm = Assert.IsType<ArmCaseNode>(Assert.Single(matchStatement.Arms));
        var textPattern = Assert.IsType<PatternLiteralTextNode>(caseArm.Pattern);

        Assert.Equal("x", textPattern.value);
        Assert.Equal(TextLiteralKind.literal_char, textPattern.literal_kind);
    }
}


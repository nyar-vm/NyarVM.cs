using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Statement;
using Std.Data.Text.Valkyrie.AST.Term;
using Std.Data.Text.Valkyrie.Semantic;

namespace Valkyrie.Tests.LanguageTests;

public sealed class SpreadAndFieldAssignTests
{
    [Fact]
    public void Parse_DoubleDotSpread_ProducesTermSpreadExpression()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main(lst: list<i32>) -> i32 {
                ..lst
            }
            """,
            "spread_double.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var spreadExpr = Assert.IsType<TermSpreadExpression>(Assert.Single(function.Body!.Statements));

        Assert.True(spreadExpr.IsDoubleDot);
        var target = Assert.IsType<IdentifierNode>(spreadExpr.Target);
        Assert.Equal("lst", target.Name);
    }

    [Fact]
    public void Parse_TripleDotSpread_ProducesTermSpreadExpressionWithIsDoubleDotFalse()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main(d: dict<string, i32>) -> i32 {
                ...d
            }
            """,
            "spread_triple.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var spreadExpr = Assert.IsType<TermSpreadExpression>(Assert.Single(function.Body!.Statements));

        Assert.False(spreadExpr.IsDoubleDot);
        var target = Assert.IsType<IdentifierNode>(spreadExpr.Target);
        Assert.Equal("d", target.Name);
    }

    [Fact]
    public void Parse_FieldAssignment_SetsFieldViaTermDotExpression()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            struct Point {
                x: i32
                y: i32
            }

            micro main(pt: Point) {
                pt.x = 42
            }
            """,
            "field_assign.v").Value!;

        Assert.Equal(2, ast.Declarations.Count);
        var function = Assert.IsType<DeclareMicro>(ast.Declarations[1]);

        var assignStmt = Assert.IsType<AssignmentStatement>(Assert.Single(function.Body!.Statements));
        var binary = Assert.IsType<TermBinaryExpression>(assignStmt.Expression);

        Assert.Equal(TermBinaryOperator.Assign, binary.Operator);
        var left = Assert.IsType<TermDotExpression>(binary.Left);
        var right = Assert.IsType<TermLiteralIntNode>(binary.Right);

        Assert.Equal("42", right.Data);
    }

    [Fact]
    public void Parse_SimpleSymbolAssignment_StillWorks()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main() {
                let x: i32
                x = 10
            }
            """,
            "simple_assign.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        Assert.Equal(2, function.Body!.Statements.Count);
        var assignStmt = Assert.IsType<AssignmentStatement>(function.Body.Statements[1]);

        var binary = Assert.IsType<TermBinaryExpression>(assignStmt.Expression);
        Assert.Equal(TermBinaryOperator.Assign, binary.Operator);
        var left = Assert.IsType<IdentifierNode>(binary.Left);
        Assert.Equal("x", left.Name);
    }

    [Fact]
    public void Parse_TupleOrdinalAssignment_UsesTermDotExpression()
    {
        var analyzer = new SourceAnalyzer();
        var ast = analyzer.Parse(
            """
            micro main(pair: (i32, i32)) {
                pair.2 = 42
            }
            """,
            "tuple_ordinal_assign.v").Value!;

        var function = Assert.IsType<DeclareMicro>(Assert.Single(ast.Declarations));
        var assignStmt = Assert.IsType<AssignmentStatement>(Assert.Single(function.Body!.Statements));
        var binary = Assert.IsType<TermBinaryExpression>(assignStmt.Expression);
        var left = Assert.IsType<TermDotExpression>(binary.Left);
        var right = Assert.IsType<TermLiteralIntNode>(binary.Right);

        Assert.Equal("2", left.Callee.FullName);
        Assert.Equal("42", right.Data);
    }
}

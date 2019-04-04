using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie;
using Std.Data.Text.Valkyrie.AST.Pattern;
using Std.Data.Text.Valkyrie.Lexer;
using Std.Data.Text.Valkyrie.Parser;

namespace Valkyrie.Tests.ParserTests;

public sealed class LoopParserTests
{
    [Fact]
    public void Parse_LoopIn_ShouldRemainLoopInStatement()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main(items: Items) {
                         loop item in items {
                             item
                         }
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        var loop = Assert.IsType<LoopInStatement>(Assert.Single(function.body!.statements));

        Assert.Equal("item", loop.iterator_name);
        Assert.NotNull(loop.iterable);
        Assert.Single(loop.body.statements);
    }

    [Fact]
    public void Parse_LoopIn_WithBreakAndContinue_ShouldKeepControlFlowStatements()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main(items: Items) {
                         loop item in items {
                             continue;
                             break;
                         }
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        var loop = Assert.IsType<LoopInStatement>(Assert.Single(function.body!.statements));

        Assert.Collection(
            loop.body.statements,
            statement => Assert.IsType<ContinueStatement>(statement),
            statement => Assert.IsType<BreakStatement>(statement));
    }

    [Fact]
    public void Parse_LoopIn_WithTuplePattern_ShouldKeepTuplePatternHeader()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main(items: Items) {
                         loop (left, right) in items {
                             left
                             right
                         }
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        var loop = Assert.IsType<LoopInStatement>(Assert.Single(function.body!.statements));
        var tuplePattern = Assert.IsType<PatternLiteralTupleNode>(loop.iterator_pattern);

        Assert.Null(loop.iterator_name);
        Assert.NotNull(loop.iterable);
        Assert.Collection(
            tuplePattern.elements,
            element => Assert.Equal("left", Assert.IsType<PatternLiteralVariableNode>(element).name),
            element => Assert.Equal("right", Assert.IsType<PatternLiteralVariableNode>(element).name));
    }

    [Fact]
    public void Parse_While_ShouldRemainWhileStatement()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main(flag: bool) {
                         while (flag) {
                             let matched = 1;
                         }
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        var loop = Assert.IsType<WhileStatement>(Assert.Single(function.body!.statements));

        Assert.NotNull(loop.condition);
        Assert.Single(loop.body.statements);
        Assert.IsType<DeclareLet>(Assert.Single(loop.body.statements));
    }

    [Fact]
    public void Parse_Until_ShouldRemainUntilStatement()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main(done: bool) {
                         until (done) {
                             let matched = 1;
                         }
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        var loop = Assert.IsType<UntilStatement>(Assert.Single(function.body!.statements));

        Assert.NotNull(loop.condition);
        Assert.Single(loop.body.statements);
        Assert.IsType<DeclareLet>(Assert.Single(loop.body.statements));
    }

    [Fact]
    public void Parse_CountingLoop_ShouldRemainLoopStatement()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main(limit: i32) {
                         loop let i = 0; i < limit; i += 1 {
                             let matched = i;
                         }
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        var loop = Assert.IsType<LoopStatement>(Assert.Single(function.body!.statements));

        var initializer = Assert.IsType<DeclareLet>(loop.initializer);
        Assert.Equal("i", initializer.name?.name);
        Assert.NotNull(loop.condition);
        Assert.NotNull(loop.update);
        Assert.Single(loop.body.statements);
    }

    [Fact]
    public void Parse_InfiniteLoop_ShouldKeepEmptyHeader()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main() {
                         loop {
                             break;
                         }
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        var loop = Assert.IsType<LoopStatement>(Assert.Single(function.body!.statements));

        Assert.Null(loop.initializer);
        Assert.Null(loop.condition);
        Assert.Null(loop.update);
        Assert.Collection(loop.body.statements, statement => Assert.IsType<BreakStatement>(statement));
    }

    [Fact]
    public void Parse_YieldStatements_ShouldKeepYieldKinds()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main() {
                         yield 1;
                         yield break;
                         yield return 2;
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));

        Assert.Collection(
            function.body!.statements,
            statement =>
            {
                var yieldStatement = Assert.IsType<YieldStatement>(statement);
                Assert.Equal(YieldKeyword.Yield, yieldStatement.keyword);
                Assert.NotNull(yieldStatement.value);
            },
            statement =>
            {
                var yieldStatement = Assert.IsType<YieldStatement>(statement);
                Assert.Equal(YieldKeyword.YieldBreak, yieldStatement.keyword);
                Assert.Null(yieldStatement.value);
            },
            statement =>
            {
                var yieldStatement = Assert.IsType<YieldStatement>(statement);
                Assert.Equal(YieldKeyword.YieldReturn, yieldStatement.keyword);
                Assert.NotNull(yieldStatement.value);
            });
    }

    [Fact]
    public void Parse_InterpolationLikeString_ShouldRemainPlainTextLiteral()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main() {
                         let msg = "slot={slot + 1}";
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        var let = Assert.IsType<DeclareLet>(Assert.Single(function.body!.statements));
        var literal = Assert.IsType<TermLiteralTextNode>(let.initializer);

        Assert.Equal("slot={slot + 1}", literal.value);
        Assert.Equal(TextLiteralKind.literal_text, literal.literal_kind);
    }

    [Fact]
    public void Parse_PrefixedStringLiteral_ShouldKeepPrefixAndContent()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main() {
                         let regex = re"(.)";
                         let custom = sdsdf"";
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        Assert.Equal(2, function.body!.statements.Count);

        var regex = Assert.IsType<DeclareLet>(function.body.statements[0]);
        var regexLiteral = Assert.IsType<TermLiteralTextNode>(regex.initializer);
        Assert.Equal("re", regexLiteral.prefix);
        Assert.Equal("(.)", regexLiteral.value);

        var custom = Assert.IsType<DeclareLet>(function.body.statements[1]);
        var customLiteral = Assert.IsType<TermLiteralTextNode>(custom.initializer);
        Assert.Equal("sdsdf", customLiteral.prefix);
        Assert.Equal(string.Empty, customLiteral.value);
    }

    [Fact]
    public void Parse_RawInterpolatedString_ShouldBuildConcatenationWithToStringCall()
    {
        var diagnostics = new DiagnosticSink();
        var source = """
                     micro main(slot: i32) {
                         let msg = r"{slot}";
                     }
                     """;

        var lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var unit = Assert.IsType<CompilationUnit>(parser.parse(lexer.tokenize(source)));

        Assert.False(diagnostics.has_errors, format_diagnostics(diagnostics));

        var function = Assert.IsType<DeclareMicro>(Assert.Single(unit.declarations));
        var let = Assert.IsType<DeclareLet>(Assert.Single(function.body!.statements));
        var concat = Assert.IsType<TermBinaryExpression>(let.initializer);
        Assert.Equal(TermBinaryOperator.addition, concat.@operator);

        var left = Assert.IsType<TermLiteralTextNode>(concat.left);
        Assert.Equal(string.Empty, left.value);

        var toStringCall = Assert.IsType<TermDotExpression>(concat.right);
        Assert.NotNull(toStringCall.call_body);
        Assert.Equal("to_string", toStringCall.callee.name);

        var slotRef = Assert.IsType<TermLiteralNamePathNode>(toStringCall.caller);
        Assert.Equal("slot", slotRef.path.name);
    }

    private static string format_diagnostics(DiagnosticSink diagnostics)
    {
        return string.Join(
            Environment.NewLine,
            diagnostics.messages.Select(diagnostic => $"{diagnostic.severity}: {diagnostic.message} @ {diagnostic.span}"));
    }
}

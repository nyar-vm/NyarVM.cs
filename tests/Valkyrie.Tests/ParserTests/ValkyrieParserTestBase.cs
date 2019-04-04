using Std.Data.Text.Valkyrie;
using Std.Data.Text.Diagnostics;

namespace Valkyrie.Tests.ParserTests;

public abstract class ValkyrieParserTestBase
{
    protected CompilationUnit parse_with_timeout(
        string source,
        ValkyrieLanguage? language = null,
        DiagnosticSink? diagnostics = null)
    {
        diagnostics ??= new DiagnosticSink();
        language ??= ValkyrieLanguage.standard;
        var lexer = new ValkyrieLexer(language, diagnostics);
        var parser = new ValkyrieParser(language, diagnostics);
        var tokens = lexer.tokenize(source);
        var result = parser.parse(tokens);
        return Assert.IsType<CompilationUnit>(result);
    }

    protected static void assert_parse_result_not_null(CompilationUnit result)
    {
        Assert.NotNull(result);
    }

    protected static void assert_no_errors(DiagnosticSink diagnostics)
    {
        var detail = string.Join(
            Environment.NewLine,
            diagnostics.get_diagnostics().Select(d => $"{d.severity}: {d.message} @ {d.span}"));
        Assert.False(diagnostics.has_errors, $"语法分析产生了意外的错误：\n{detail}");
    }

    protected static void assert_error_count(DiagnosticSink diagnostics, int expectedCount)
    {
        Assert.Equal(expectedCount, diagnostics.get_diagnostics().Count(d => d.severity == DiagnosticSeverity.error));
    }
}

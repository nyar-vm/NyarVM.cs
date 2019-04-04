using Std.Data.Text.Diagnostics;
using Std.Data.Text.Valkyrie;
using Std.Data.Text.Valkyrie.AST;
using Std.Data.Text.Valkyrie.Lexer;
using Std.Data.Text.Valkyrie.Parser;

namespace Nyar.Tests.Command;

public sealed class CommandSystemIntegrationTests
{
    private static string ReadSourceFile(string relativePath)
    {
        var stdVPath = Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..", "..",
            "valkyrie.v", "projects", relativePath);
        return File.ReadAllText(stdVPath);
    }

    private static ParseResult ParseSource(string source)
    {
        var diagnostics = new DiagnosticSink();
        var lang = ValkyrieLanguage.standard;
        var lexer = new ValkyrieLexer(lang, diagnostics);
        var tokens = lexer.tokenize(source);

        var parser = new ValkyrieParser(lang, diagnostics);
        var ast = parser.parse(tokens);

        return new ParseResult(ast, diagnostics);
    }

    private static void AssertNoErrors(ParseResult result, string fileName)
    {
        Assert.NotNull(result.Ast);
        if (result.Diagnostics.has_errors)
        {
            var diags = result.Diagnostics.messages;
            var messages = string.Join("\n", diags.Select(d => $"  [{d.severity}] {d.message}"));
            Assert.Fail($"解析 {fileName} 时出现诊断错误:\n{messages}");
        }
    }

    [Fact]
    public void Parse_ModelV_Success_NoErrors()
    {
        var source = ReadSourceFile("std/source/command/model.v");
        var result = ParseSource(source);
        AssertNoErrors(result, "model.v");
    }

    [Fact]
    public void Parse_ConvertV_Success_NoErrors()
    {
        var source = ReadSourceFile("std/source/command/convert.v");
        var result = ParseSource(source);
        AssertNoErrors(result, "convert.v");
    }

    [Fact]
    public void Parse_AppV_Success_NoErrors()
    {
        var source = ReadSourceFile("std/source/command/app.v");
        var result = ParseSource(source);
        AssertNoErrors(result, "app.v");
    }

    [Fact]
    public void Parse_HelpV_Success_NoErrors()
    {
        var source = ReadSourceFile("std/source/command/help.v");
        var result = ParseSource(source);
        AssertNoErrors(result, "help.v");
    }

    [Fact(Skip = "Valkyrie Parser 尚未支持 attribute 关键字语法")]
    public void Parse_MarkerV_WithNewAttributes_Success_NoErrors()
    {
        var source = ReadSourceFile("core/source/marker/_.v");
        var result = ParseSource(source);
        AssertNoErrors(result, "marker.v");
    }

    [Fact]
    public void ConvertV_HasValidAst()
    {
        var source = ReadSourceFile("std/source/command/convert.v");
        var result = ParseSource(source);

        Assert.NotNull(result.Ast);
        Assert.IsType<ProgramRoot>(result.Ast);
    }

    [Fact]
    public void HelpV_HasValidAst()
    {
        var source = ReadSourceFile("std/source/command/help.v");
        var result = ParseSource(source);

        Assert.NotNull(result.Ast);
        Assert.IsType<ProgramRoot>(result.Ast);
    }

    private sealed record ParseResult(ValkyrieNode Ast, DiagnosticSink Diagnostics);
}

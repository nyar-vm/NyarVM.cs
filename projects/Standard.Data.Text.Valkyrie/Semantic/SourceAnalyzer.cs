using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;
using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.Lexer;
using Std.Data.Text.Valkyrie.Parser;
using CompilationUnit = Std.Data.Text.Valkyrie.AST.ProgramRoot;

namespace Std.Data.Text.Valkyrie.Semantic;

/// <summary>
///     源码分析器 —— Lex + Parse 一步完成
/// </summary>
public sealed class SourceAnalyzer
{
    private readonly ValkyrieLexer _lexer;

    /// <summary>
    ///     创建源码分析器
    /// </summary>
    public SourceAnalyzer()
    {
        diagnostics = new DiagnosticSink();
        _lexer = new ValkyrieLexer(ValkyrieLanguage.standard, diagnostics);
    }

    /// <summary>
    ///     诊断信息收集器
    /// </summary>
    public DiagnosticSink diagnostics { get; }

    /// <summary>
    ///     词法分析
    /// </summary>
    /// <param name="source">源码文本</param>
    /// <returns>词法单元列表</returns>
    public IReadOnlyList<GreenLeafNode> lex(string source)
    {
        diagnostics.clear();
        return _lexer.tokenize(source);
    }

    /// <summary>
    ///     对源码文本执行 Lex → Parse，返回 CompilationUnit 和诊断信息
    /// </summary>
    /// <param name="source">源码文本</param>
    /// <param name="filePath">源文件路径，用于诊断定位</param>
    /// <returns>包含 CompilationUnit 和诊断列表的解析结果</returns>
    public ParseResult<CompilationUnit> parse(string source, string filePath = "")
    {
        diagnostics.clear();

        var tokens = _lexer.tokenize(source);

        if (tokens is not { Count: > 0 }) return ParseResult<CompilationUnit>.fail(diagnostics.messages);

        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var result = parser.parse(tokens);

        if (result is CompilationUnit compilationUnit)
        {
            if (!string.IsNullOrEmpty(filePath)) compilationUnit = compilationUnit with { file_path = filePath };
            return ParseResult<CompilationUnit>.ok(compilationUnit, diagnostics.messages);
        }

        return ParseResult<CompilationUnit>.fail(diagnostics.messages);
    }

    /// <summary>
    ///     对已词法分析的 Token 列表执行 Parse
    /// </summary>
    /// <param name="tokens">词法单元列表</param>
    /// <param name="filePath">源文件路径</param>
    /// <returns>包含 CompilationUnit 和诊断列表的解析结果</returns>
    public ParseResult<CompilationUnit> parse(IReadOnlyList<GreenLeafNode> tokens, string filePath = "")
    {
        diagnostics.clear();

        var parser = new ValkyrieParser(ValkyrieLanguage.standard, diagnostics);
        var result = parser.parse(tokens);

        if (result is CompilationUnit compilationUnit)
        {
            if (!string.IsNullOrEmpty(filePath)) compilationUnit = compilationUnit with { file_path = filePath };
            return ParseResult<CompilationUnit>.ok(compilationUnit, diagnostics.messages);
        }

        return ParseResult<CompilationUnit>.fail(diagnostics.messages);
    }
}
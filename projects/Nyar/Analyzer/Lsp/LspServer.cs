using SemanticSymbolKind = Nyar.Analyzer.Semantic.SymbolKind;

namespace Nyar.Analyzer.Lsp;

public enum LspMessageType
{
    error = 1,
    warning = 2,
    info = 3,
    log = 4
}

public readonly struct LspPosition
{
    public int line { get; }
    public int character { get; }

    public LspPosition(int line, int character)
    {
        this.line = line;
        this.character = character;
    }
}

public readonly struct LspRange
{
    public LspPosition start { get; }
    public LspPosition end { get; }

    public LspRange(LspPosition start, LspPosition end)
    {
        this.start = start;
        this.end = end;
    }
}

/// <summary>
///     诊断严重级别
/// </summary>
public enum DiagnosticSeverity
{
    error = 1,
    warning = 2,
    info = 3,
    hint = 4
}

public readonly struct LspDiagnostic
{
    public LspRange range { get; }
    public DiagnosticSeverity severity { get; }
    public string message { get; }
    public string? code { get; }
    public string source { get; }

    public LspDiagnostic(LspRange range, DiagnosticSeverity severity, string message, string source,
        string? code = null)
    {
        this.range = range;
        this.severity = severity;
        this.message = message;
        this.source = source;
        this.code = code;
    }
}

public readonly struct LspCompletionItem
{
    public string label { get; }
    public SemanticSymbolKind kind { get; }
    public string? detail { get; }

    public LspCompletionItem(string label, SemanticSymbolKind kind, string? detail = null)
    {
        this.label = label;
        this.kind = kind;
        this.detail = detail;
    }
}

public readonly struct LspLocation
{
    public string file_uri { get; }
    public LspRange range { get; }

    public LspLocation(string fileUri, LspRange range)
    {
        file_uri = fileUri;
        this.range = range;
    }
}

public readonly struct LspHover
{
    public LspRange range { get; }
    public string contents { get; }

    public LspHover(LspRange range, string contents)
    {
        this.range = range;
        this.contents = contents;
    }
}

// TODO: 待实现 - LspServer 依赖的 SemanticModel、Symbol、ISymbol、IType、SemanticDiagnostic、
// SemanticHighlighter、SemanticIndex 等类型的 API 与当前实际定义不匹配（PascalCase vs snake_case 命名差异）。
// 待语义分析类型 API 稳定后恢复。
//
// 原始设计：LSP 协议服务器，处理代码补全、跳转定义、引用查找、诊断推送等功能
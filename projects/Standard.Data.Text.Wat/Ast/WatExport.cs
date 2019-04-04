using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     导出声明
/// </summary>
/// <param name="Name">导出名。</param>
/// <param name="ExportKind">导出种类。</param>
/// <param name="Index">索引。</param>
/// <param name="Span">源码位置。</param>
public sealed record WatExport(string Name, string ExportKind, uint Index, TextSpan Span = default) : WatAstNode(Span);
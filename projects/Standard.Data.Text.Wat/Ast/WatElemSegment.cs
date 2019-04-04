using Std.Data.Text.Syntax;

namespace Oak.Wat.Ast;

/// <summary>
///     WAT 元素段
/// </summary>
/// <param name="Span">源码位置。</param>
/// <param name="Table">表标识。</param>
/// <param name="Offset">偏移表达式。</param>
/// <param name="Elements">元素列表。</param>
public sealed record WatElemSegment(TextSpan Span, string? Table, string? Offset, List<string> Elements)
    : WatAstNode(Span);
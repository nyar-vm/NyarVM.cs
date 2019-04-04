using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     If 语句


/// </summary>
public sealed record CIf(
    CAstNode condition,
    CAstNode then_body,
    CAstNode? else_body,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);
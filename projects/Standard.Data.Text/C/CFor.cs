using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     For 循环语句


/// </summary>
public sealed record CFor(
    CAstNode? init,
    CAstNode? condition,
    CAstNode? increment,
    CAstNode body,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);
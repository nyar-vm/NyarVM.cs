using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     While 循环语句


/// </summary>
public sealed record CWhile(
    CAstNode condition,
    CAstNode body,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);
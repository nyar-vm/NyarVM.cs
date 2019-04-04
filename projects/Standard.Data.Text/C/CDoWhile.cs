using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     Do-While 循环语句


/// </summary>
public sealed record CDoWhile(
    CAstNode body,
    CAstNode condition,
    TextSpan Span = default(TextSpan))
    : CAstNode(Span);
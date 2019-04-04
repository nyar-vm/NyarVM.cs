using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     Return 语句


/// </summary>
public sealed record CReturn(CAstNode? value, TextSpan Span = default(TextSpan))
    : CAstNode(Span);
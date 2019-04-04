using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     下标访问表达式


/// </summary>
public sealed record CSubscript(CAstNode @object, CAstNode index, TextSpan Span = default(TextSpan))
    : CAstNode(Span);
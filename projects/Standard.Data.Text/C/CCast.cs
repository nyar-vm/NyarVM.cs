using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     类型转换表达式


/// </summary>
public sealed record CCast(CAstNode type, CAstNode expression, TextSpan Span = default(TextSpan))
    : CAstNode(Span);
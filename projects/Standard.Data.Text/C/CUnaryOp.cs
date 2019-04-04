using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     一元运算表达式


/// </summary>
public sealed record CUnaryOp(string @operator, CAstNode operand, TextSpan Span = default(TextSpan))
    : CAstNode(Span);
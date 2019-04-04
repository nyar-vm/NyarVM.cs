using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

/// <summary>

///     三元条件表达式


/// </summary>
public sealed record CTernaryOp(CAstNode condition, CAstNode then_expr, CAstNode else_expr, TextSpan Span = default(TextSpan))
    : CAstNode(Span);
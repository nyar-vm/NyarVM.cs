using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

#region 表达式

/// <summary>
///     二元运算表达式
/// </summary>
public sealed record CBinaryOp(CAstNode left, string @operator, CAstNode right, TextSpan Span = default(TextSpan)): CAstNode(Span);

#endregion
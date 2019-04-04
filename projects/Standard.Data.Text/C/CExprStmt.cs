using Std.Data.Text.Syntax;

namespace Std.Data.Text.C;

#region 语句


/// <summary>

///     表达式语句


/// </summary>
public sealed record CExprStmt(CAstNode expression, TextSpan Span = default(TextSpan))
    : CAstNode(Span);

#endregion
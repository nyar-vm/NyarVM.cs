using Std.Data.Text.Syntax;

namespace Std.Data.Text.Julia.AST;

#region 语句

public sealed record JlIfExpr(
    JlAstNode condition,
    JlAstNode then_branch,
    IReadOnlyList<(JlAstNode Condition, JlAstNode Body)> else_if_branches,
    JlAstNode? else_branch,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlForExpr(
    IReadOnlyList<(JlAstNode Iterator, JlAstNode Iterable)> iterators,
    JlAstNode body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlWhileExpr(
    JlAstNode condition,
    JlAstNode body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTryExpr(
    JlAstNode body,
    IReadOnlyList<(JlAstNode Pattern, JlAstNode Body)> catch_clauses,
    JlAstNode? finally_body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlLetExpr(
    IReadOnlyList<JlAstNode> bindings,
    JlAstNode body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlDoBlock(
    JlAstNode call,
    IReadOnlyList<JlAstNode> parameters,
    JlAstNode body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlReturnExpr(
    JlAstNode? value,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlBreakExpr(TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlContinueExpr(TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlBlock(
    IReadOnlyList<JlAstNode> statements,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlAssignment(
    JlAstNode left,
    JlAstNode right,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlCompoundAssignment(
    JlAstNode left,
    string @operator,
    JlAstNode right,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

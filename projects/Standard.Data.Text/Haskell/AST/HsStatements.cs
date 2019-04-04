namespace Std.Data.Text.Haskell.AST;

#region 语句

public sealed record HsDoBlock(
    IReadOnlyList<HsAstNode> Statements,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsDoBind(
    HsAstNode Pattern,
    HsAstNode Expression,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsDoLet(
    IReadOnlyList<HsAstNode> Declarations,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsLetExpr(
    IReadOnlyList<HsAstNode> Declarations,
    HsAstNode Body,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsIfExpr(
    HsAstNode Condition,
    HsAstNode ThenBranch,
    HsAstNode ElseBranch,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsCaseExpr(
    HsAstNode Scrutinee,
    IReadOnlyList<HsAlternative> Alternatives,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsAlternative(
    HsAstNode Pattern,
    IReadOnlyList<HsAstNode> Guards,
    HsAstNode Body,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsWhereClause(
    IReadOnlyList<HsAstNode> Declarations,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

#endregion

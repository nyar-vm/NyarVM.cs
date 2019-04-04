namespace Std.Data.Text.OCaml.AST;

#region 语句

public sealed record OcIfExpr(
    OcAstNode Condition,
    OcAstNode ThenBranch,
    OcAstNode ElseBranch,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcMatchExpr(
    OcAstNode Scrutinee,
    IReadOnlyList<OcMatchCase> Cases,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcMatchCase(
    OcAstNode Pattern,
    OcAstNode? Guard,
    OcAstNode Body,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcTryExpr(
    OcAstNode Expression,
    IReadOnlyList<OcMatchCase> Cases,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcForExpr(
    string Iterator,
    OcAstNode Start,
    OcAstNode End,
    bool IsDownto,
    OcAstNode Body,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcWhileExpr(
    OcAstNode Condition,
    OcAstNode Body,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcLetExpr(
    bool IsRec,
    OcAstNode Pattern,
    OcAstNode Body,
    OcAstNode InExpr,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcFunExpr(
    IReadOnlyList<OcAstNode> Parameters,
    OcAstNode Body,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcSequence(
    OcAstNode First,
    OcAstNode Second,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

#endregion

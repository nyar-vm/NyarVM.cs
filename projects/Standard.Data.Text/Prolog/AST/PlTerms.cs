namespace Std.Data.Text.Prolog.AST;

#region 程序结构

public sealed record PlProgram(
    IReadOnlyList<PlAstNode> Clauses,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlFact(
    PlAstNode Head,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlRule(
    PlAstNode Head,
    IReadOnlyList<PlAstNode> Body,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlQuery(
    IReadOnlyList<PlAstNode> Goals,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlDirective(
    string Name,
    IReadOnlyList<PlAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

#endregion

#region 项与复合项

public sealed record PlAtom(string Name, TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlVariable(string Name, TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlNumber(string Value, TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlString(string Value, TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlCompound(
    string Functor,
    IReadOnlyList<PlAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlList(
    IReadOnlyList<PlAstNode> Elements,
    PlAstNode? Tail,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlTuple(
    IReadOnlyList<PlAstNode> Elements,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

#endregion

#region 运算符与控制

public sealed record PlBinaryOp(
    PlAstNode Left,
    string Operator,
    PlAstNode Right,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlUnaryOp(
    string Operator,
    PlAstNode Operand,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlIfThenElse(
    PlAstNode Condition,
    PlAstNode ThenBranch,
    PlAstNode ElseBranch,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlCut(TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlFail(TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlTrue(TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlNot(PlAstNode Goal, TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlConjunction(
    IReadOnlyList<PlAstNode> Goals,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

public sealed record PlDisjunction(
    IReadOnlyList<PlAstNode> Goals,
    TextSpan Span = default(TextSpan)) : PlAstNode(Span);

#endregion

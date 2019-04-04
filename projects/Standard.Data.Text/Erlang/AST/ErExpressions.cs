namespace Std.Data.Text.Erlang.AST;

#region 表达式

public sealed record ErAtom(string Name, TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErVariable(string Name, TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErNumber(string Value, TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErString(string Value, TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErChar(string Value, TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErTuple(IReadOnlyList<ErAstNode> Elements, TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErList(IReadOnlyList<ErAstNode> Elements, ErAstNode? Tail, TextSpan Span = default(TextSpan))
    : ErAstNode(Span);

public sealed record ErBinary(IReadOnlyList<ErAstNode> Segments, TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErMap(IReadOnlyList<(ErAstNode Key, ErAstNode Value)> Pairs, TextSpan Span = default(TextSpan))
    : ErAstNode(Span);

public sealed record ErMapUpdate(
    ErAstNode Base,
    IReadOnlyList<(ErAstNode Key, ErAstNode Value)> Pairs,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErRecord(
    string Name,
    IReadOnlyList<(string Field, ErAstNode Value)> Fields,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErRecordAccess(
    ErAstNode Record,
    string Name,
    string Field,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErRecordUpdate(
    ErAstNode Record,
    string Name,
    IReadOnlyList<(string Field, ErAstNode Value)> Fields,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

#endregion

#region 运算与调用

public sealed record ErBinaryOp(
    ErAstNode Left,
    string Operator,
    ErAstNode Right,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErUnaryOp(
    string Operator,
    ErAstNode Operand,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErCall(
    ErAstNode Function,
    IReadOnlyList<ErAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErRemoteCall(
    string Module,
    string Function,
    IReadOnlyList<ErAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErFun(
    string? Module,
    string Name,
    int Arity,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErLambda(
    IReadOnlyList<ErClause> Clauses,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

#endregion

#region 控制流

public sealed record ErCase(
    ErAstNode Expression,
    IReadOnlyList<ErClause> Clauses,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErIf(
    IReadOnlyList<ErClause> Clauses,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErReceive(
    IReadOnlyList<ErClause> Clauses,
    ErAstNode? After,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErTry(
    ErAstNode Expression,
    IReadOnlyList<ErClause> CatchClauses,
    ErAstNode? After,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErBlock(
    IReadOnlyList<ErAstNode> Expressions,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErSend(
    ErAstNode Process,
    ErAstNode Message,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErReceiveAfter(
    ErAstNode Timeout,
    ErAstNode Body,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

#endregion

#region 模式

public sealed record ErMatch(
    ErAstNode Pattern,
    ErAstNode Expression,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErCons(
    ErAstNode Head,
    ErAstNode Tail,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErWildCard(TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErBinaryPattern(
    IReadOnlyList<ErAstNode> Segments,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErBinarySegment(
    ErAstNode Value,
    ErAstNode? Size,
    IReadOnlyList<(string Name, ErAstNode Value)> Specifiers,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

#endregion

#region 类型表达式

public sealed record ErTypeVar(string Name, TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErTypeAtom(string Name, TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErTypeApp(
    ErAstNode Constructor,
    IReadOnlyList<ErAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErFunctionType(
    IReadOnlyList<ErAstNode> Parameters,
    ErAstNode ReturnType,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErUnionType(
    IReadOnlyList<ErAstNode> Types,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErTupleType(
    IReadOnlyList<ErAstNode> Elements,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErListType(
    ErAstNode ElementType,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErMapType(
    IReadOnlyList<(ErAstNode Key, ErAstNode Value)> Pairs,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErRangeType(
    ErAstNode Lower,
    ErAstNode Upper,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

#endregion

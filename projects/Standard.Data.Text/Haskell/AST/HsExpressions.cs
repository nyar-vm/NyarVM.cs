namespace Std.Data.Text.Haskell.AST;

#region 基础表达式

public sealed record HsIdentifier(string Name, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsQualified(string Module, string Name, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsLiteral(string Kind, string Value, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsList(IReadOnlyList<HsAstNode> Elements, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsTuple(IReadOnlyList<HsAstNode> Elements, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsUnit(TextSpan Span = default(TextSpan)) : HsAstNode(Span);

#endregion

#region 运算表达式

public sealed record HsBinaryOp(
    HsAstNode Left,
    string Operator,
    HsAstNode Right,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsUnaryOp(
    string Operator,
    HsAstNode Operand,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsInfixApp(
    HsAstNode Left,
    HsAstNode Operator,
    HsAstNode Right,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsNegApp(HsAstNode Operand, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

#endregion

#region 函数与抽象

public sealed record HsApplication(
    HsAstNode Function,
    IReadOnlyList<HsAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsLambda(
    IReadOnlyList<HsAstNode> Patterns,
    HsAstNode Body,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsLeftSection(
    HsAstNode Operand,
    string Operator,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsRightSection(
    string Operator,
    HsAstNode Operand,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

#endregion

#region 模式

public sealed record HsVarPattern(string Name, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsConPattern(
    string Name,
    IReadOnlyList<HsAstNode> Fields,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsListPattern(IReadOnlyList<HsAstNode> Elements, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsTuplePattern(IReadOnlyList<HsAstNode> Elements, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsWildCardPattern(TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsAsPattern(string Name, HsAstNode Pattern, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsIrrefutablePattern(HsAstNode Pattern, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

#endregion

#region 类型表达式

public sealed record HsTypeVar(string Name, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsTypeCon(string Name, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsTypeApp(
    HsAstNode Function,
    HsAstNode Argument,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsFunctionType(
    HsAstNode Argument,
    HsAstNode Result,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsForAllType(
    IReadOnlyList<string> TypeVars,
    HsAstNode Type,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsConstraintType(
    IReadOnlyList<HsAstNode> Constraints,
    HsAstNode Type,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsTupleType(IReadOnlyList<HsAstNode> Elements, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsListType(HsAstNode ElementType, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

#endregion

namespace Std.Data.Text.OCaml.AST;

#region 基础表达式

public sealed record OcIdentifier(string Name, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcQualified(string Module, string Name, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcLiteral(string Kind, string Value, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcList(IReadOnlyList<OcAstNode> Elements, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcTuple(IReadOnlyList<OcAstNode> Elements, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcUnit(TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcArray(IReadOnlyList<OcAstNode> Elements, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

#endregion

#region 运算表达式

public sealed record OcBinaryOp(
    OcAstNode Left,
    string Operator,
    OcAstNode Right,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcUnaryOp(
    string Operator,
    OcAstNode Operand,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

#endregion

#region 调用与访问

public sealed record OcApplication(
    OcAstNode Function,
    IReadOnlyList<OcAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcFieldAccess(
    OcAstNode Record,
    string Field,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcRecordUpdate(
    OcAstNode Record,
    IReadOnlyList<(string Field, OcAstNode Value)> Updates,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcIndexAccess(
    OcAstNode Array,
    OcAstNode Index,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

#endregion

#region 模式

public sealed record OcVarPattern(string Name, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcConstructorPattern(
    string Name,
    IReadOnlyList<OcAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcTuplePattern(IReadOnlyList<OcAstNode> Elements, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcListPattern(IReadOnlyList<OcAstNode> Elements, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcRecordPattern(
    IReadOnlyList<(string Field, OcAstNode Pattern)> Fields,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcWildCardPattern(TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcConsPattern(
    OcAstNode Head,
    OcAstNode Tail,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcAliasPattern(
    string Name,
    OcAstNode Pattern,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcLazyPattern(
    OcAstNode Pattern,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

#endregion

#region 类型表达式

public sealed record OcTypeVar(string Name, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcTypeCon(string Name, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcTypeApp(
    OcAstNode Constructor,
    IReadOnlyList<OcAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcFunctionType(
    OcAstNode Parameter,
    OcAstNode Result,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcTupleType(IReadOnlyList<OcAstNode> Elements, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcListType(OcAstNode ElementType, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcOptionType(OcAstNode InnerType, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcPolyVariantType(
    IReadOnlyList<OcAstNode> Constructors,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcObjectType(
    IReadOnlyList<(string Name, OcAstNode Type)> Methods,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

#endregion

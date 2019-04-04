using Std.Data.Text.Syntax;

namespace Std.Data.Text.Julia.AST;

#region 基础表达式

public sealed record JlIdentifier(string name, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlLiteral(string kind, string value, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTuple(IReadOnlyList<JlAstNode> elements, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlArray(IReadOnlyList<JlAstNode> elements, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlDict(IReadOnlyList<(JlAstNode Key, JlAstNode Value)> pairs, TextSpan Span = default(TextSpan))
    : JlAstNode(Span);

public sealed record JlSet(IReadOnlyList<JlAstNode> elements, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlUnit(TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlSymbol(string name, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlRange(
    JlAstNode start,
    JlAstNode? step,
    JlAstNode end,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlComprehension(
    JlAstNode expression,
    IReadOnlyList<(JlAstNode Iterator, JlAstNode Iterable)> iterators,
    bool is_generator,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlMacroCall(
    string name,
    IReadOnlyList<JlAstNode> arguments,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlCommand(string value, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 运算表达式

public sealed record JlBinaryOp(
    JlAstNode left,
    string @operator,
    JlAstNode right,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlUnaryOp(
    string @operator,
    JlAstNode operand,
    bool is_prefix,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlBroadcastOp(
    JlAstNode left,
    string @operator,
    JlAstNode right,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlPipe(
    JlAstNode left,
    JlAstNode right,
    bool is_reverse,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTernary(
    JlAstNode condition,
    JlAstNode then_branch,
    JlAstNode else_branch,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 调用与索引

public sealed record JlCall(
    JlAstNode function,
    IReadOnlyList<JlAstNode> arguments,
    IReadOnlyList<JlAstNode> keyword_arguments,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlIndex(
    JlAstNode @object,
    IReadOnlyList<JlAstNode> indices,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlFieldAccess(
    JlAstNode @object,
    string field,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlPropertyAccess(
    JlAstNode @object,
    string property,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlQualifiedAccess(
    string module,
    string name,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlLambda(
    IReadOnlyList<JlAstNode> parameters,
    JlAstNode body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 类型表达式

public sealed record JlTypeVar(string name, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTypeCon(string name, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTypeApp(
    JlAstNode constructor,
    IReadOnlyList<JlAstNode> arguments,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlUnionType(
    IReadOnlyList<JlAstNode> types,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTupleType(
    IReadOnlyList<JlAstNode> elements,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlWhereType(
    JlAstNode type,
    IReadOnlyList<JlAstNode> constraints,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlFunctionType(
    IReadOnlyList<JlAstNode> parameters,
    JlAstNode return_type,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlNamedTupleType(
    IReadOnlyList<(string Name, JlAstNode Type)> fields,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

namespace Std.Data.Text.Erlang.AST;

#region 模块与属性

public sealed record ErModule(
    string Name,
    IReadOnlyList<ErAstNode> Attributes,
    IReadOnlyList<ErAstNode> Functions,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErAttribute(
    string Name,
    IReadOnlyList<ErAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErExport(
    IReadOnlyList<(string Name, int Arity)> Functions,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErImport(
    string Module,
    IReadOnlyList<(string Name, int Arity)> Functions,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErSpec(
    string Name,
    int Arity,
    IReadOnlyList<ErAstNode> Types,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErTypeDecl(
    string Name,
    IReadOnlyList<string> TypeParams,
    ErAstNode Type,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErRecordDecl(
    string Name,
    IReadOnlyList<(string Field, ErAstNode? Type)> Fields,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

#endregion

#region 函数定义

public sealed record ErFunction(
    string Name,
    int Arity,
    IReadOnlyList<ErClause> Clauses,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

public sealed record ErClause(
    IReadOnlyList<ErAstNode> Patterns,
    IReadOnlyList<ErAstNode> Guards,
    ErAstNode Body,
    TextSpan Span = default(TextSpan)) : ErAstNode(Span);

#endregion

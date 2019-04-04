using Std.Data.Text.Syntax;

namespace Std.Data.Text.Julia.AST;

#region 模块与导入

public sealed record JlModule(
    string name,
    IReadOnlyList<JlAstNode> exports,
    IReadOnlyList<JlAstNode> imports,
    IReadOnlyList<JlAstNode> statements,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlImport(
    bool is_using,
    string module,
    IReadOnlyList<string> names,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlExport(IReadOnlyList<string> names, TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 函数与宏

public sealed record JlFunctionDef(
    string name,
    IReadOnlyList<JlAstNode> parameters,
    IReadOnlyList<string> where_params,
    JlAstNode? return_type,
    JlAstNode body,
    bool is_short,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlMacroDef(
    string name,
    IReadOnlyList<JlAstNode> parameters,
    JlAstNode body,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlParameter(
    string name,
    JlAstNode? type_annotation,
    JlAstNode? default_value,
    bool is_varargs,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlKeywordParameter(
    string name,
    JlAstNode? type_annotation,
    JlAstNode? default_value,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

#region 类型声明

public sealed record JlStructDef(
    string name,
    IReadOnlyList<string> type_params,
    IReadOnlyList<JlAstNode> fields,
    bool is_mutable,
    bool is_abstract,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlField(
    string name,
    JlAstNode? type_annotation,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

public sealed record JlTypeDef(
    string name,
    IReadOnlyList<string> type_params,
    JlAstNode? super_type,
    TextSpan Span = default(TextSpan)) : JlAstNode(Span);

#endregion

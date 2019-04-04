namespace Std.Data.Text.OCaml.AST;

#region 模块与结构

public sealed record OcModule(
    string Name,
    IReadOnlyList<OcAstNode> Declarations,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcModuleType(
    string Name,
    IReadOnlyList<OcAstNode> Signatures,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcOpen(string ModulePath, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcInclude(OcAstNode ModuleExpr, TextSpan Span = default(TextSpan)) : OcAstNode(Span);

#endregion

#region 类型声明

public sealed record OcTypeDecl(
    string Name,
    IReadOnlyList<string> TypeParams,
    IReadOnlyList<OcAstNode> Constructors,
    bool IsPrivate,
    bool IsRecursive,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcVariantConstructor(
    string Name,
    IReadOnlyList<OcAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcRecordField(
    string Name,
    bool Mutable,
    OcAstNode FieldType,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcExceptionDecl(
    string Name,
    IReadOnlyList<OcAstNode> Arguments,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

#endregion

#region 值与函数绑定

public sealed record OcLetBinding(
    bool IsRec,
    OcAstNode Pattern,
    IReadOnlyList<OcAstNode> Parameters,
    OcAstNode Body,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcValBinding(
    OcAstNode Pattern,
    OcAstNode Body,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

public sealed record OcTypeConstraint(
    OcAstNode Expression,
    OcAstNode Type,
    TextSpan Span = default(TextSpan)) : OcAstNode(Span);

#endregion

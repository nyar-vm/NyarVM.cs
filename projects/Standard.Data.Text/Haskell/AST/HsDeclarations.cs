namespace Std.Data.Text.Haskell.AST;

#region 模块与导入

public sealed record HsModule(
    string Name,
    IReadOnlyList<HsAstNode> Exports,
    IReadOnlyList<HsAstNode> Imports,
    IReadOnlyList<HsAstNode> Declarations,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsImport(
    bool Qualified,
    string Module,
    string? Alias,
    bool Hiding,
    IReadOnlyList<string>? Names,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsExport(string Name, TextSpan Span = default(TextSpan)) : HsAstNode(Span);

#endregion

#region 数据与类型声明

public sealed record HsDataDecl(
    string Name,
    IReadOnlyList<string> TypeVars,
    IReadOnlyList<HsAstNode> Constructors,
    IReadOnlyList<string> Deriving,
    bool IsNewtype,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsConstructor(
    string Name,
    IReadOnlyList<HsAstNode> Fields,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsTypeDecl(
    string Name,
    IReadOnlyList<string> TypeVars,
    HsAstNode Type,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsTypeClassDecl(
    string Name,
    IReadOnlyList<string> TypeVars,
    IReadOnlyList<HsAstNode> SuperClasses,
    IReadOnlyList<HsAstNode> Methods,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsInstanceDecl(
    IReadOnlyList<string> Constraints,
    string ClassName,
    IReadOnlyList<HsAstNode> Types,
    IReadOnlyList<HsAstNode> Definitions,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsTypeSignature(
    string Name,
    IReadOnlyList<string> Constraints,
    HsAstNode Type,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsTypeAnnotation(
    HsAstNode Subject,
    HsAstNode Type,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

#endregion

#region 函数与绑定

public sealed record HsFunctionDecl(
    string Name,
    IReadOnlyList<HsAstNode> Patterns,
    IReadOnlyList<HsAstNode> Guards,
    HsAstNode Body,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsGuard(
    HsAstNode Condition,
    HsAstNode Body,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

public sealed record HsPatternBind(
    HsAstNode Pattern,
    HsAstNode Body,
    TextSpan Span = default(TextSpan)) : HsAstNode(Span);

#endregion

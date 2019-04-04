using Std.Data.Text.Syntax;

namespace Std.Data.Text.Rust;

#region 璇彞


/// <summary>

///     琛ㄨ揪寮忚鍙?


/// </summary>
public sealed record RustExprStmt(RustAstNode expression, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     let 缁戝畾璇彞


/// </summary>
public sealed record RustLetStmt(
    RustAstNode pattern,
    RustAstNode? type,
    RustAstNode? initializer,
    bool is_mutable,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     return 璇彞


/// </summary>
public sealed record RustReturnStmt(RustAstNode? value, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     break 璇彞


/// </summary>
public sealed record RustBreakStmt(RustAstNode? value, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     continue 璇彞


/// </summary>
public sealed record RustContinueStmt(TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     while 寰幆


/// </summary>
public sealed record RustWhileStmt(RustAstNode condition, RustAstNode body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     loop 寰幆


/// </summary>
public sealed record RustLoopStmt(RustAstNode body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     for 寰幆


/// </summary>
public sealed record RustForStmt(RustAstNode pattern, RustAstNode iterator, RustAstNode body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     函数定义


/// </summary>
public sealed record RustFunctionDef(
    string name,
    IReadOnlyList<RustParam> parameters,
    RustAstNode? return_type,
    RustAstNode body,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     鍑芥暟鍙傛暟


/// </summary>
public sealed record RustParam(string name, RustAstNode? type, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     结构体定义


/// </summary>
public sealed record RustStructDef(
    string name,
    IReadOnlyList<RustFieldDef> fields,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     结构体字段


/// </summary>
public sealed record RustFieldDef(string name, RustAstNode type, bool is_public, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     枚举定义


/// </summary>
public sealed record RustEnumDef(
    string name,
    IReadOnlyList<RustVariantDef> variants,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     鏋氫妇鍙樹綋


/// </summary>
public sealed record RustVariantDef(string name, IReadOnlyList<RustAstNode>? fields, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     impl 鍧?


/// </summary>
public sealed record RustImplDef(
    RustAstNode? trait,
    RustAstNode type,
    IReadOnlyList<RustAstNode> members,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     trait 定义


/// </summary>
public sealed record RustTraitDef(
    string name,
    IReadOnlyList<RustAstNode> members,
    TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     绫诲瀷鍒悕 (type)


/// </summary>
public sealed record RustTypeAlias(string name, RustAstNode type, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     use 声明


/// </summary>
public sealed record RustUseDecl(string path, string? alias, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     mod 声明


/// </summary>
public sealed record RustModDecl(string name, RustAstNode? body, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     类型节点


/// </summary>
public sealed record RustTypeNode(string name, bool is_reference, bool is_mutable, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);


/// <summary>

///     翻译单元（Crate 根节点）


/// </summary>
public sealed record RustCrate(IReadOnlyList<RustAstNode> items, TextSpan Span = default(TextSpan))
    : RustAstNode(Span);

#endregion

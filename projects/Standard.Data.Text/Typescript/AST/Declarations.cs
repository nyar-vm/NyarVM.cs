using Std.Data.Text.Syntax;

namespace Std.Data.Text.Typescript.AST;

#region 模块声明


/// <summary>

///     编译单元（根节点）

/// </summary>
public sealed record TsCompilationUnit(IReadOnlyList<TsAstNode> declarations, TextSpan Span)
    : TsAstNode(Span)
{
    public TsCompilationUnit(IReadOnlyList<TsAstNode> declarations)
        : this(declarations, default(TextSpan))
    {
    }
}


/// <summary>

///     瀵煎叆声明


/// </summary>
public sealed record TsImportDecl(
    string module_path,
    string? alias,
    bool is_type_only,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsImportDecl(string module_path, string? alias, bool is_type_only)
        : this(module_path, alias, is_type_only, default(TextSpan))
    {
    }
}


/// <summary>

///     瀵煎嚭声明


/// </summary>
public sealed record TsExportDecl(string name, TsAstNode value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsExportDecl(string name, TsAstNode value)
        : this(name, value, default(TextSpan))
    {
    }
}


/// <summary>

///     命名空间声明


/// </summary>
public sealed record TsNamespaceDecl(
    string name,
    IReadOnlyList<TsAstNode> members,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsNamespaceDecl(string name, IReadOnlyList<TsAstNode> members)
        : this(name, members, default(TextSpan))
    {
    }
}

#endregion

#region 变量与函数声明

/// <summary>

///     变量声明


/// </summary>
public sealed record TsVariableDecl(
    string name,
    TsAstNode? type_annotation,
    TsAstNode? initializer,
    bool is_const,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsVariableDecl(string name, TsAstNode? type_annotation, TsAstNode? initializer, bool is_const)
        : this(name, type_annotation, initializer, is_const, default(TextSpan))
    {
    }
}


/// <summary>

///     鍑芥暟声明


/// </summary>
public sealed record TsFunctionDecl(
    string name,
    IReadOnlyList<TsParameter> parameters,
    TsAstNode? return_type,
    TsAstNode body,
    bool is_async,
    bool is_generator,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsFunctionDecl(
        string name,
        IReadOnlyList<TsParameter> parameters,
        TsAstNode? return_type,
        TsAstNode body,
        bool is_async,
        bool is_generator
    )
        : this(name, parameters, return_type, body, is_async, is_generator, default(TextSpan))
    {
    }
}

#endregion

#region 绫诲瀷声明


/// <summary>

///     类声明

/// </summary>
public sealed record TsClassDecl(
    string name,
    IReadOnlyList<TsAstNode> members,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsClassDecl(string name, IReadOnlyList<TsAstNode> members)
        : this(name, members, default(TextSpan))
    {
    }
}


/// <summary>

///     接口声明


/// </summary>
public sealed record TsInterfaceDecl(
    string name,
    IReadOnlyList<TsAstNode> members,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsInterfaceDecl(string name, IReadOnlyList<TsAstNode> members)
        : this(name, members, default(TextSpan))
    {
    }
}


/// <summary>

///     绫诲瀷鍒悕声明


/// </summary>
public sealed record TsTypeAliasDecl(string name, TsAstNode type, TextSpan Span)
    : TsAstNode(Span)
{
    public TsTypeAliasDecl(string name, TsAstNode type)
        : this(name, type, default(TextSpan))
    {
    }
}


/// <summary>

///     鏋氫妇声明


/// </summary>
public sealed record TsEnumDecl(
    string name,
    IReadOnlyList<TsEnumMember> members,
    bool is_const,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsEnumDecl(string name, IReadOnlyList<TsEnumMember> members, bool is_const)
        : this(name, members, is_const, default(TextSpan))
    {
    }
}


/// <summary>

///     鏋氫妇鎴愬憳


/// </summary>
public sealed record TsEnumMember(string name, TsAstNode? initializer, TextSpan Span)
    : TsAstNode(Span)
{
    public TsEnumMember(string name, TsAstNode? initializer)
        : this(name, initializer, default(TextSpan))
    {
    }
}

#endregion


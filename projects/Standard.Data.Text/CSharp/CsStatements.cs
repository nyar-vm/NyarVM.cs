namespace Std.Data.Text.CSharp;

#region 语句


/// <summary>

///     表达式语句


/// </summary>
public sealed record CsExprStmt(CsAstNode Expression, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     变量声明


/// </summary>
public sealed record CsVarDecl(CsAstNode Type, string Name, CsAstNode? Initializer, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     If 语句


/// </summary>
public sealed record CsIf(
    CsAstNode Condition,
    CsAstNode ThenBody,
    CsAstNode? ElseBody,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     While 循环语句


/// </summary>
public sealed record CsWhile(
    CsAstNode Condition,
    CsAstNode Body,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Do-While 循环语句


/// </summary>
public sealed record CsDoWhile(
    CsAstNode Body,
    CsAstNode Condition,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     For 循环语句


/// </summary>
public sealed record CsFor(
    CsAstNode? Init,
    CsAstNode? Condition,
    CsAstNode? Increment,
    CsAstNode Body,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     foreach 循环语句


/// </summary>
public sealed record CsForEach(
    CsAstNode Type,
    string VariableName,
    CsAstNode Collection,
    CsAstNode Body,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Return 语句


/// </summary>
public sealed record CsReturn(CsAstNode? Value, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Break 语句


/// </summary>
public sealed record CsBreak(TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Continue 语句


/// </summary>
public sealed record CsContinue(TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Throw 语句


/// </summary>
public sealed record CsThrow(CsAstNode? Expression, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Try-Catch-Finally 语句


/// </summary>
public sealed record CsTry(
    CsAstNode TryBlock,
    IReadOnlyList<CsCatchClause> Catches,
    CsAstNode? FinallyBlock,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Catch 子句


/// </summary>
public sealed record CsCatchClause(
    CsAstNode? ExceptionType,
    string? ExceptionName,
    CsAstNode Body,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Using 语句


/// </summary>
public sealed record CsUsingStmt(CsAstNode Resource, CsAstNode Body, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Lock 语句


/// </summary>
public sealed record CsLock(CsAstNode Expression, CsAstNode Body, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Yield return 语句


/// </summary>
public sealed record CsYieldReturn(CsAstNode Value, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Yield break 语句


/// </summary>
public sealed record CsYieldBreak(TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Switch 语句


/// </summary>
public sealed record CsSwitch(
    CsAstNode Expression,
    IReadOnlyList<CsAstNode> Sections,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Switch 节（含标签和语句列表）


/// </summary>
public sealed record CsSwitchSection(
    IReadOnlyList<CsAstNode> Labels,
    IReadOnlyList<CsAstNode> Statements,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Case 标签


/// </summary>
public sealed record CsCaseLabel(CsAstNode Value, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Default 标签


/// </summary>
public sealed record CsDefaultLabel(TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     复合语句（代码块）


/// </summary>
public sealed record CsBlock(IReadOnlyList<CsAstNode> Statements, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);

#endregion

#region 声明


/// <summary>

///     编译单元（根节点）


/// </summary>
public sealed record CsCompilationUnit(
    IReadOnlyList<CsUsingDirective> Usings,
    IReadOnlyList<CsAstNode> Declarations,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     using 指令


/// </summary>
public sealed record CsUsingDirective(string Namespace, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     命名空间声明


/// </summary>
public sealed record CsNamespace(
    string Name,
    IReadOnlyList<CsUsingDirective> Usings,
    IReadOnlyList<CsAstNode> Declarations,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     类声明


/// </summary>
public sealed record CsClassDecl(
    string Name,
    CsAstNode? BaseType,
    IReadOnlyList<CsAstNode> Interfaces,
    IReadOnlyList<CsAstNode> Members,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     结构体声明


/// </summary>
public sealed record CsStructDecl(
    string Name,
    IReadOnlyList<CsAstNode> Interfaces,
    IReadOnlyList<CsAstNode> Members,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     接口声明


/// </summary>
public sealed record CsInterfaceDecl(
    string Name,
    IReadOnlyList<CsAstNode> BaseInterfaces,
    IReadOnlyList<CsAstNode> Members,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     枚举声明


/// </summary>
public sealed record CsEnumDecl(
    string Name,
    CsAstNode? BaseType,
    IReadOnlyList<CsEnumMember> Members,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     枚举成员


/// </summary>
public sealed record CsEnumMember(string Name, CsAstNode? Value, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Record 声明


/// </summary>
public sealed record CsRecordDecl(
    string Name,
    IReadOnlyList<CsParamDecl> Parameters,
    CsAstNode? BaseType,
    IReadOnlyList<CsAstNode> Members,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     方法声明


/// </summary>
public sealed record CsMethodDecl(
    CsAstNode ReturnType,
    string Name,
    IReadOnlyList<CsParamDecl> Parameters,
    CsAstNode? Body,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     构造函数声明


/// </summary>
public sealed record CsConstructorDecl(
    string Name,
    IReadOnlyList<CsParamDecl> Parameters,
    CsAstNode? Initializer,
    CsAstNode? Body,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     属性声明


/// </summary>
public sealed record CsPropertyDecl(
    CsAstNode Type,
    string Name,
    CsAstNode? Getter,
    CsAstNode? Setter,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     字段声明


/// </summary>
public sealed record CsFieldDecl(
    CsAstNode Type,
    string Name,
    CsAstNode? Initializer,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     委托声明


/// </summary>
public sealed record CsDelegateDecl(
    CsAstNode ReturnType,
    string Name,
    IReadOnlyList<CsParamDecl> Parameters,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     事件声明


/// </summary>
public sealed record CsEventDecl(
    CsAstNode Type,
    string Name,
    IReadOnlyList<string> Modifiers,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     参数声明


/// </summary>
public sealed record CsParamDecl(
    CsAstNode Type,
    string Name,
    CsAstNode? DefaultValue,
    string? Modifier,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     类型节点


/// </summary>
public sealed record CsTypeNode(
    string Name,
    bool IsNullable,
    bool IsArray,
    IReadOnlyList<CsAstNode>? TypeArguments,
    TextSpan Span = default(TextSpan))
    : CsAstNode(Span);

#endregion

namespace Std.Data.Text.CSharp;

#region 表达式


/// <summary>

///     二元运算表达式


/// </summary>
public sealed record CsBinaryOp(CsAstNode Left, string Operator, CsAstNode Right, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     一元运算表达式


/// </summary>
public sealed record CsUnaryOp(string Operator, CsAstNode Operand, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     三元条件表达式


/// </summary>
public sealed record CsTernaryOp(CsAstNode Condition, CsAstNode ThenExpr, CsAstNode ElseExpr, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     字面量表达式


/// </summary>
public sealed record CsLiteral(string Kind, string Value, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     标识符表达式


/// </summary>
public sealed record CsIdentifier(string Name, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     函数调用表达式


/// </summary>
public sealed record CsCall(CsAstNode Function, IReadOnlyList<CsAstNode> Arguments, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     成员访问表达式


/// </summary>
public sealed record CsMemberAccess(CsAstNode Object, string Member, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     条件成员访问表达式（?.）


/// </summary>
public sealed record CsConditionalAccess(CsAstNode Object, string Member, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     下标访问表达式


/// </summary>
public sealed record CsSubscript(CsAstNode Object, CsAstNode Index, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     类型转换表达式


/// </summary>
public sealed record CsCast(CsAstNode Type, CsAstNode Expression, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     对象创建表达式


/// </summary>
public sealed record CsNewObject(CsAstNode Type, IReadOnlyList<CsAstNode> Arguments, CsAstNode? Initializer, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     数组创建表达式


/// </summary>
public sealed record CsNewArray(CsAstNode Type, IReadOnlyList<CsAstNode> Sizes, CsAstNode? Initializer, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     typeof 表达式


/// </summary>
public sealed record CsTypeOf(CsAstNode Type, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     default 表达式


/// </summary>
public sealed record CsDefault(CsAstNode? Type, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     Lambda 表达式


/// </summary>
public sealed record CsLambda(IReadOnlyList<CsParamDecl> Parameters, CsAstNode Body, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     this 表达式


/// </summary>
public sealed record CsThis(TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     base 表达式


/// </summary>
public sealed record CsBase(TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     空合并表达式（??）


/// </summary>
public sealed record CsNullCoalesce(CsAstNode Left, CsAstNode Right, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     is 表达式


/// </summary>
public sealed record CsIs(CsAstNode Expression, CsAstNode Type, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     as 表达式


/// </summary>
public sealed record CsAs(CsAstNode Expression, CsAstNode Type, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     初始化列表


/// </summary>
public sealed record CsInitList(IReadOnlyList<CsAstNode> Elements, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     逐字标识符（@keyword）


/// </summary>
public sealed record CsVerbatimIdentifier(string Name, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     checked/unchecked 表达式


/// </summary>
public sealed record CsChecked(CsAstNode Expression, bool IsChecked, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     sizeof 表达式


/// </summary>
public sealed record CsSizeOf(CsAstNode Type, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     nameof 表达式


/// </summary>
public sealed record CsNameOf(CsAstNode Expression, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     with 表达式


/// </summary>
public sealed record CsWith(CsAstNode Expression, CsAstNode Initializer, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     switch 表达式


/// </summary>
public sealed record CsSwitchExpression(CsAstNode GoverningExpression, IReadOnlyList<CsAstNode> Arms, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     switch 表达式分支


/// </summary>
public sealed record CsSwitchArm(CsAstNode Pattern, CsAstNode Expression, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     范围表达式（..）


/// </summary>
public sealed record CsRange(CsAstNode? Start, CsAstNode? End, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     元组表达式


/// </summary>
public sealed record CsTuple(IReadOnlyList<CsAstNode> Elements, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     丢弃表达式（_）


/// </summary>
public sealed record CsDiscard(TextSpan Span = default(TextSpan))
    : CsAstNode(Span);


/// <summary>

///     索引表达式（^）


/// </summary>
public sealed record CsIndexFromEnd(CsAstNode Expression, TextSpan Span = default(TextSpan))
    : CsAstNode(Span);

#endregion

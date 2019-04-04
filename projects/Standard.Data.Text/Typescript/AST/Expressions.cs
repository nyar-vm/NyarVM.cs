using Std.Data.Text.Syntax;

namespace Std.Data.Text.Typescript.AST;

#region 鍩虹表达式

/// <summary>

///     鏍囪瘑绗﹁〃杈惧紡


/// </summary>
public sealed record TsIdentifier(string name, TextSpan Span)
    : TsAstNode(Span)
{
    public TsIdentifier(string name)
        : this(name, default(TextSpan))
    {
    }
}


/// <summary>

///     字面量表达式


/// </summary>
public sealed record TsLiteral(string kind, string value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsLiteral(string kind, string value)
        : this(kind, value, default(TextSpan))
    {
    }
}


/// <summary>

///     this 表达式

/// </summary>
public sealed record TsThisExpr(TextSpan Span)
    : TsAstNode(Span)
{
    public TsThisExpr()
        : this(default(TextSpan))
    {
    }
}


/// <summary>

///     super 表达式

/// </summary>
public sealed record TsSuperExpr(TextSpan Span)
    : TsAstNode(Span)
{
    public TsSuperExpr()
        : this(default(TextSpan))
    {
    }
}

#endregion

#region 杩愮畻表达式

/// <summary>

///     二元表达式

/// </summary>
public sealed record TsBinaryExpr(
    TsAstNode left,
    string @operator,
    TsAstNode right,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsBinaryExpr(TsAstNode left, string @operator, TsAstNode right)
        : this(left, @operator, right, default(TextSpan))
    {
    }
}


/// <summary>

///     一元表达式


/// </summary>
public sealed record TsUnaryExpr(
    string @operator,
    TsAstNode operand,
    bool is_prefix,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsUnaryExpr(string @operator, TsAstNode operand, bool is_prefix)
        : this(@operator, operand, is_prefix, default(TextSpan))
    {
    }
}


/// <summary>

///     赋值表达式


/// </summary>
public sealed record TsAssignmentExpr(
    TsAstNode left,
    string @operator,
    TsAstNode right,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsAssignmentExpr(TsAstNode left, string @operator, TsAstNode right)
        : this(left, @operator, right, default(TextSpan))
    {
    }
}


/// <summary>

///     鏉′欢琛ㄨ揪寮忥紙涓夊厓杩愮畻绗︼級


/// </summary>
public sealed record TsConditionalExpr(
    TsAstNode condition,
    TsAstNode then_branch,
    TsAstNode else_branch,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsConditionalExpr(TsAstNode condition, TsAstNode then_branch, TsAstNode else_branch)
        : this(condition, then_branch, else_branch, default(TextSpan))
    {
    }
}


/// <summary>

///     typeof 表达式

/// </summary>
public sealed record TsTypeofExpr(TsAstNode operand, TextSpan Span)
    : TsAstNode(Span)
{
    public TsTypeofExpr(TsAstNode operand)
        : this(operand, default(TextSpan))
    {
    }
}


/// <summary>

///     instanceof 表达式

/// </summary>
public sealed record TsInstanceofExpr(
    TsAstNode left,
    TsAstNode right,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsInstanceofExpr(TsAstNode left, TsAstNode right)
        : this(left, right, default(TextSpan))
    {
    }
}


/// <summary>

///     yield 表达式

/// </summary>
public sealed record TsYieldExpr(TsAstNode? value, bool is_delegate, TextSpan Span)
    : TsAstNode(Span)
{
    public TsYieldExpr(TsAstNode? value, bool is_delegate)
        : this(value, is_delegate, default(TextSpan))
    {
    }
}

#endregion

#region 璋冪敤涓庢垚鍛樿闂?

/// <summary>

///     璋冪敤表达式

/// </summary>
public sealed record TsCallExpr(
    TsAstNode callee,
    IReadOnlyList<TsAstNode> arguments,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsCallExpr(TsAstNode callee, IReadOnlyList<TsAstNode> arguments)
        : this(callee, arguments, default(TextSpan))
    {
    }
}


/// <summary>

///     鎴愬憳表达式

/// </summary>
public sealed record TsMemberExpr(TsAstNode @object, string member, TextSpan Span)
    : TsAstNode(Span)
{
    public TsMemberExpr(TsAstNode @object, string member)
        : this(@object, member, default(TextSpan))
    {
    }
}


/// <summary>

///     灞炴€ц闂〃杈惧紡


/// </summary>
public sealed record TsPropertyAccess(TsAstNode @object, string property, TextSpan Span)
    : TsAstNode(Span)
{
    public TsPropertyAccess(TsAstNode @object, string property)
        : this(@object, property, default(TextSpan))
    {
    }
}


/// <summary>

///     元素访问表达式

/// </summary>
public sealed record TsElementAccess(
    TsAstNode @object,
    TsAstNode index,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsElementAccess(TsAstNode @object, TsAstNode index)
        : this(@object, index, default(TextSpan))
    {
    }
}


/// <summary>

///     new 表达式

/// </summary>
public sealed record TsNewExpr(
    TsAstNode callee,
    IReadOnlyList<TsAstNode> arguments,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsNewExpr(TsAstNode callee, IReadOnlyList<TsAstNode> arguments)
        : this(callee, arguments, default(TextSpan))
    {
    }
}

#endregion

#region 函数与集合表达式


/// <summary>

///     绠ご鍑芥暟表达式

/// </summary>
public sealed record TsArrowFunctionExpr(
    IReadOnlyList<TsParameter> parameters,
    TsAstNode? return_type,
    TsAstNode body,
    bool is_async,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsArrowFunctionExpr(
        IReadOnlyList<TsParameter> parameters,
        TsAstNode? return_type,
        TsAstNode body,
        bool is_async
    )
        : this(parameters, return_type, body, is_async, default(TextSpan))
    {
    }
}


/// <summary>

///     鍑芥暟表达式

/// </summary>
public sealed record TsFunctionExpr(
    string? name,
    IReadOnlyList<TsParameter> parameters,
    TsAstNode? return_type,
    TsAstNode body,
    bool is_async,
    bool is_generator,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsFunctionExpr(
        string? name,
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


/// <summary>

///     类型表达式


/// </summary>
public sealed record TsClassExpr(
    string? name,
    IReadOnlyList<TsAstNode> members,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsClassExpr(string? name, IReadOnlyList<TsAstNode> members)
        : this(name, members, default(TextSpan))
    {
    }
}


/// <summary>

///     鏁扮粍字面量

/// </summary>
public sealed record TsArrayLiteral(
    IReadOnlyList<TsAstNode> elements,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsArrayLiteral(IReadOnlyList<TsAstNode> elements)
        : this(elements, default(TextSpan))
    {
    }
}


/// <summary>

///     瀵硅薄字面量

/// </summary>
public sealed record TsObjectLiteral(
    IReadOnlyList<TsProperty> properties,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsObjectLiteral(IReadOnlyList<TsProperty> properties)
        : this(properties, default(TextSpan))
    {
    }
}


/// <summary>

///     模板字面量插值

/// </summary>
public sealed record TsTemplateLiteral(
    IReadOnlyList<TsAstNode> parts,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsTemplateLiteral(IReadOnlyList<TsAstNode> parts)
        : this(parts, default(TextSpan))
    {
    }
}


/// <summary>

///     展开元素


/// </summary>
public sealed record TsSpreadElement(TsAstNode argument, TextSpan Span)
    : TsAstNode(Span)
{
    public TsSpreadElement(TsAstNode argument)
        : this(argument, default(TextSpan))
    {
    }
}

#endregion


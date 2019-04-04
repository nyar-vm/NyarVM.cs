using Std.Data.Text.Syntax;

namespace Std.Data.Text.Typescript.AST;

#region 鍧椾笌鏉′欢语句


/// <summary>

///     鍧楄鍙?

/// </summary>
public sealed record TsBlockStmt(IReadOnlyList<TsAstNode> statements, TextSpan Span)
    : TsAstNode(Span)
{
    public TsBlockStmt(IReadOnlyList<TsAstNode> statements)
        : this(statements, default(TextSpan))
    {
    }
}


/// <summary>

///     if 鏉′欢语句


/// </summary>
public sealed record TsIfStmt(
    TsAstNode condition,
    TsAstNode then_block,
    TsAstNode? else_block,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsIfStmt(TsAstNode condition, TsAstNode then_block, TsAstNode? else_block)
        : this(condition, then_block, else_block, default(TextSpan))
    {
    }
}


/// <summary>

///     switch 语句


/// </summary>
public sealed record TsSwitchStmt(
    TsAstNode expression,
    IReadOnlyList<TsSwitchCase> cases,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsSwitchStmt(TsAstNode expression, IReadOnlyList<TsSwitchCase> cases)
        : this(expression, cases, default(TextSpan))
    {
    }
}


/// <summary>

///     switch case 子句


/// </summary>
public sealed record TsSwitchCase(
    TsAstNode? test,
    IReadOnlyList<TsAstNode> statements,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsSwitchCase(TsAstNode? test, IReadOnlyList<TsAstNode> statements)
        : this(test, statements, default(TextSpan))
    {
    }
}

#endregion

#region 寰幆语句


/// <summary>

///     for 寰幆语句


/// </summary>
public sealed record TsForStmt(
    TsAstNode? init,
    TsAstNode? condition,
    TsAstNode? increment,
    TsAstNode body,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsForStmt(TsAstNode? init, TsAstNode? condition, TsAstNode? increment, TsAstNode body)
        : this(init, condition, increment, body, default(TextSpan))
    {
    }
}


/// <summary>

///     while 寰幆语句


/// </summary>
public sealed record TsWhileStmt(TsAstNode condition, TsAstNode body, TextSpan Span)
    : TsAstNode(Span)
{
    public TsWhileStmt(TsAstNode condition, TsAstNode body)
        : this(condition, body, default(TextSpan))
    {
    }
}


/// <summary>

///     do-while 寰幆语句


/// </summary>
public sealed record TsDoWhileStmt(TsAstNode body, TsAstNode condition, TextSpan Span)
    : TsAstNode(Span)
{
    public TsDoWhileStmt(TsAstNode body, TsAstNode condition)
        : this(body, condition, default(TextSpan))
    {
    }
}


/// <summary>

///     for-in 语句


/// </summary>
public sealed record TsForInStmt(
    TsAstNode left,
    TsAstNode right,
    TsAstNode body,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsForInStmt(TsAstNode left, TsAstNode right, TsAstNode body)
        : this(left, right, body, default(TextSpan))
    {
    }
}


/// <summary>

///     for-of 语句


/// </summary>
public sealed record TsForOfStmt(
    TsAstNode left,
    TsAstNode right,
    TsAstNode body,
    bool is_await,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsForOfStmt(TsAstNode left, TsAstNode right, TsAstNode body, bool is_await)
        : this(left, right, body, is_await, default(TextSpan))
    {
    }
}

#endregion

#region 璺宠浆涓庢帶鍒惰鍙?

/// <summary>

///     return 语句


/// </summary>
public sealed record TsReturnStmt(TsAstNode? value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsReturnStmt(TsAstNode? value)
        : this(value, default(TextSpan))
    {
    }
}


/// <summary>

///     throw 语句


/// </summary>
public sealed record TsThrowStmt(TsAstNode value, TextSpan Span)
    : TsAstNode(Span)
{
    public TsThrowStmt(TsAstNode value)
        : this(value, default(TextSpan))
    {
    }
}


/// <summary>

///     break 语句


/// </summary>
public sealed record TsBreakStmt(string? label, TextSpan Span)
    : TsAstNode(Span)
{
    public TsBreakStmt(string? label)
        : this(label, default(TextSpan))
    {
    }
}


/// <summary>

///     continue 语句


/// </summary>
public sealed record TsContinueStmt(string? label, TextSpan Span)
    : TsAstNode(Span)
{
    public TsContinueStmt(string? label)
        : this(label, default(TextSpan))
    {
    }
}

#endregion

#region 异常处理语句


/// <summary>

///     try-catch-finally 语句


/// </summary>
public sealed record TsTryStmt(
    TsAstNode block,
    TsCatchClause? catch_clause,
    TsAstNode? finally_block,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsTryStmt(TsAstNode block, TsCatchClause? catch_clause, TsAstNode? finally_block)
        : this(block, catch_clause, finally_block, default(TextSpan))
    {
    }
}


/// <summary>

///     catch 子句


/// </summary>
public sealed record TsCatchClause(
    string? parameter_name,
    TsAstNode? parameter_type,
    TsAstNode block,
    TextSpan Span
) : TsAstNode(Span)
{
    public TsCatchClause(string? parameter_name, TsAstNode? parameter_type, TsAstNode block)
        : this(parameter_name, parameter_type, block, default(TextSpan))
    {
    }
}

#endregion

#region 其他语句


/// <summary>

///     琛ㄨ揪寮忚鍙?

/// </summary>
public sealed record TsExprStmt(TsAstNode expression, TextSpan Span)
    : TsAstNode(Span)
{
    public TsExprStmt(TsAstNode expression)
        : this(expression, default(TextSpan))
    {
    }
}


/// <summary>

///     debugger 语句


/// </summary>
public sealed record TsDebuggerStmt(TextSpan Span)
    : TsAstNode(Span)
{
    public TsDebuggerStmt()
        : this(default(TextSpan))
    {
    }
}


/// <summary>

///     绌鸿鍙?

/// </summary>
public sealed record TsEmptyStmt(TextSpan Span)
    : TsAstNode(Span)
{
    public TsEmptyStmt()
        : this(default(TextSpan))
    {
    }
}


/// <summary>

///     鏍囩语句


/// </summary>
public sealed record TsLabeledStmt(string label, TsAstNode statement, TextSpan Span)
    : TsAstNode(Span)
{
    public TsLabeledStmt(string label, TsAstNode statement)
        : this(label, statement, default(TextSpan))
    {
    }
}

#endregion


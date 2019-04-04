using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     代码块语句，由一对大括号包裹的语句序列
/// </summary>
/// <para>示例：</para>
/// <code>
/// {
///     let x = 1;
///     let y = 2;
///     x + y
/// }
/// </code>
public sealed record FunctionBody : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public FunctionBody()
    {
    }

    /// <summary>
    ///     带语句列表的构造函数
    /// </summary>
    public FunctionBody(IReadOnlyList<ValkyrieNode> statements)
    {
        this.statements = statements;
    }

    /// <summary>
    ///     带语句列表和位置的构造函数
    /// </summary>
    public FunctionBody(IReadOnlyList<ValkyrieNode> statements, TextSpan span)
    {
        this.statements = statements;
    }

    /// <summary>
    ///     代码块内的语句列表
    /// </summary>
    public IReadOnlyList<ValkyrieNode> statements { get; init; } = [];
}
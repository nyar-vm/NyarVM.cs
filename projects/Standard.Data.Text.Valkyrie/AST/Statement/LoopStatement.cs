using Std.Data.Text.Syntax;

namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     For 循环语句
/// </summary>
/// <para>支持标准三段式 for 循环：</para>
/// <code>
/// loop var i = 0; i &lt; 10; i += 1 {
///     print(i);
/// }
/// </code>
/// <para>所有三个表达式均可省略：</para>
/// <code>
/// for ;; {
///     yield frame();
/// }
/// </code>
public sealed record LoopStatement : ValkyrieNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public LoopStatement()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public LoopStatement(ValkyrieNode? initializer, ValkyrieNode? condition, ValkyrieNode? update, FunctionBody body,
        TextSpan span)
    {
        this.initializer = initializer;
        this.condition = condition;
        this.update = update;
        this.body = body;
        this.span = span;
    }

    /// <summary>
    ///     初始化表达式（最先执行一次），可为 <c>null</c>
    /// </summary>
    public ValkyrieNode? initializer { get; init; }

    /// <summary>
    ///     条件表达式（每次迭代前检查），可为 <c>null</c> 表示无限循环
    /// </summary>
    public ValkyrieNode? condition { get; init; }

    /// <summary>
    ///     更新表达式（每次迭代后执行），可为 <c>null</c>
    /// </summary>
    public ValkyrieNode? update { get; init; }

    /// <summary>
    ///     循环体
    /// </summary>
    public FunctionBody body { get; init; } = new();
}
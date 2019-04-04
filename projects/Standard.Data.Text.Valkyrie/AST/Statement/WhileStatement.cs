namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     While 循环语句，条件为真时重复执行循环体
/// </summary>
/// <para>示例：</para>
/// <code>
/// while i &gt; 0 {
///     print(i);
///     i -= 1;
/// }
/// </code>
public sealed record WhileStatement : ValkyrieNode
{
    /// <summary>
    ///     循环条件表达式
    /// </summary>
    public ValkyrieNode condition { get; init; } = new IdentifierNode();

    /// <summary>
    ///     循环体
    /// </summary>
    public FunctionBody body { get; init; } = new();
}
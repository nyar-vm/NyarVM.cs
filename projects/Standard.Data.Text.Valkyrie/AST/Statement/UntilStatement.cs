namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Until 循环语句，条件为假时重复执行循环体
/// </summary>
/// <para>示例：</para>
/// <code>
/// until done {
///     tick();
/// }
/// </code>
public sealed record UntilStatement : ValkyrieNode
{
    /// <summary>
    ///     循环终止条件表达式
    /// </summary>
    public ValkyrieNode condition { get; init; } = new IdentifierNode();

    /// <summary>
    ///     循环体
    /// </summary>
    public FunctionBody body { get; init; } = new();
}

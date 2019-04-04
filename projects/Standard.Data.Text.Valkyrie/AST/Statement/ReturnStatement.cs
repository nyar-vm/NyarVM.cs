namespace Std.Data.Text.Valkyrie.AST.Statement;

/// <summary>
///     Return 语句，从函数中返回一个值
/// </summary>
/// <para>示例：</para>
/// <code>
/// return 42;
/// return;
/// </code>
public sealed record ReturnStatement : ValkyrieNode
{
    /// <summary>
    ///     返回的表达式值，为 <c>null</c> 时表示无返回值（返回 void）
    /// </summary>
    public ValkyrieNode? value { get; init; }
}
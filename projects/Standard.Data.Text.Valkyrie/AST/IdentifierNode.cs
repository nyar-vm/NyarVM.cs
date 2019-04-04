namespace Std.Data.Text.Valkyrie.AST;

/// <summary>
///     标识符节点，表示源代码中的一个名称引用
/// </summary>
/// <para>用于变量引用、类型名、函数名等所有命名引用场景</para>
/// <para>示例：</para>
/// <code>
/// let x = 42;
/// let player = ...;
/// player.health
/// </code>
public sealed record IdentifierNode : ValkyrieNode
{
    public IdentifierNode()
    {
    }

    public IdentifierNode(string name, bool isRaw = false)
    {
        this.name = name;
        is_raw = isRaw;
    }

    /// <summary>
    ///     标识符名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     `raw identifier`
    /// </summary>
    public bool is_raw { get; init; }
}
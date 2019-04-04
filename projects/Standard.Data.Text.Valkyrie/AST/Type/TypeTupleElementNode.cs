namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     元组类型元素，如 <c>i32</c> 或 <c>value: i32</c>
/// </summary>
public sealed record TypeTupleElementNode : ValkyrieNode
{
    /// <summary>
    ///     元组元素标签；未命名时为 <see langword="null" />
    /// </summary>
    public IdentifierNode? label { get; init; }

    /// <summary>
    ///     元组元素类型
    /// </summary>
    public TypeNode type { get; init; } = null!;
}

namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     泛型类型参数，用于约束求解器中表示类型参数及其名称
/// </summary>
public sealed record TypeParameter : ValkyrieNode
{
    /// <summary>
    ///     类型参数名称
    /// </summary>
    public string name { get; init; } = string.Empty;
}
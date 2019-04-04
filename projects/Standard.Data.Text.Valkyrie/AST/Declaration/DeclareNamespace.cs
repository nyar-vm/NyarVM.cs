namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     命名空间声明
/// </summary>
public sealed record DeclareNamespace : ValkyrieNode
{
    /// <summary>
    ///     命名空间名称（点号分隔）
    /// </summary>
    public IdentifierNode name { get; init; } = new();

    /// <summary>
    ///     是否为主命名空间
    /// </summary>
    public bool is_primary { get; init; }

    /// <summary>
    ///     是否为测试命名空间
    /// </summary>
    public bool is_test { get; init; }

    /// <summary>
    ///     命名空间内的声明列表
    /// </summary>
    public IReadOnlyList<ValkyrieNode> declarations { get; init; } = [];

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> attributes { get; init; } = [];
}
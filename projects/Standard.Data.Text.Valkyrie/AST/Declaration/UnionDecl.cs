using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     Union 联合类型声明节点，如 <c>union Shape { Circle { radius: f32 }, Rectangle { width: f32, height: f32 } }</c>
/// </summary>
public sealed record UnionDecl : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     Union 的变体列表
    /// </summary>
    public IReadOnlyList<UnionVariant> variants { get; init; } = [];

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     Union 名称
    /// </summary>
    public IdentifierNode? name { get; init; }
}

/// <summary>
///     Union 变体声明，如 <c>Circle { radius: f32 }</c>
/// </summary>
public sealed record UnionVariant : ValkyrieNode
{
    /// <summary>
    ///     变体名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     变体字段列表
    /// </summary>
    public IReadOnlyList<UnionVariantField> fields { get; init; } = [];
}

/// <summary>
///     Union 变体字段，如 <c>radius: f32</c>
/// </summary>
public sealed record UnionVariantField : ValkyrieNode
{
    /// <summary>
    ///     字段名称
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     字段类型注解
    /// </summary>
    public TypeNode field_type { get; init; } = new TypeLiteralNamePathNode();
}
namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     联合类型（Unite）声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// unite Option<T>
///         {
///         Some { value: T },
///         None,
///         }
/// </code>
public sealed record DeclareUnite : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     泛型类型参数列表
    /// </summary>
    public IReadOnlyList<TypeParameterList> type_parameters { get; init; } = [];

    /// <summary>
    ///     变体列表
    /// </summary>
    public IReadOnlyList<DeclareUniteVariant> variants { get; init; } = [];

    /// <summary>
    ///     函数列表
    /// </summary>
    public IReadOnlyList<DeclareObjectMethod> methods { get; init; } = [];

    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     联合类型名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}
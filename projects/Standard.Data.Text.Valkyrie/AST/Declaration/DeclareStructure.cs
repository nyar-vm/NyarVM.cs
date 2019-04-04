using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     结构体声明，值类型的数据聚合
/// </summary>
/// <para>示例：</para>
/// <code>
/// structure Point {
///     x: f32;
///     y: f32;
/// }
/// </code>
public sealed record DeclareStructure : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? body { get; init; } = null;

    /// <summary>
    ///     泛型类型参数
    /// </summary>
    public IReadOnlyList<TypeParameterList> type_parameters { get; init; } = [];

    /// <summary>
    ///     泛型约束
    /// </summary>
    public IReadOnlyList<GenericConstraint> generic_constraints { get; init; } = [];

    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     结构体名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}
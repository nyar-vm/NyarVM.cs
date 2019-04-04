namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     域声明，类内部的嵌套作用域，用于组织相关字段和函数
/// </summary>
/// <para>示例：</para>
/// <code>
/// class GameObject {
///     Physics {
///         var velocity: vec3;
///         var mass: f32;
///         fn apply_force(f: vec3) { ... }
///     }
/// }
/// </code>
public sealed record DeclareObjectDomain : ValkyrieNode
{
    /// <summary>
    ///     域名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> attributes { get; init; } = [];

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody body { get; init; } = new();
}
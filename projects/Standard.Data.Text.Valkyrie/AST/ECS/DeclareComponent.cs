using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Std.Data.Text.Valkyrie.AST.ECS;

/// <summary>
///     组件声明，ECS 架构中的数据载体
/// </summary>
/// <para>示例：</para>
/// <code>
/// component Position {
///     x: f32;
///     y: f32;
///     z: f32;
/// }
/// </code>
public sealed record DeclareComponent : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? body { get; init; } = null;

    /// <summary>
    ///     组件名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();
}
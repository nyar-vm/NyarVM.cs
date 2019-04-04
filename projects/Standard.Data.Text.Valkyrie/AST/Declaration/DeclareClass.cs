using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     类声明节点
/// </summary>
/// <para>示例：</para>
/// <code>
/// class Player {
///     name: utf8;
///     health: f32 = 100.0;
///     heal(mut self, amount: f32) {
///         self.health += amount;
///     }
/// }
/// </code>
/// <para>支持泛型：</para>
/// <code>
/// class Container&lt;T&gt; where T : Serializable {
///     data: T;
/// }
/// </code>
public sealed record DeclareClass : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     继承规范列表（基类、接口等）
    /// </summary>
    public InheritanceList? inheritance { get; init; } = null;

    /// <summary>
    ///     泛型类型参数列表
    /// </summary>
    public IReadOnlyList<TypeParameterList> type_parameters { get; init; } = [];

    /// <summary>
    ///     泛型约束列表（<c>where</c> 子句）
    /// </summary>
    public IReadOnlyList<GenericConstraint> generic_constraints { get; init; } = [];

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? body { get; init; } = null;

    /// <summary>
    ///     注解信息
    /// </summary>
    public Annotations annotations { get; init; } = new();

    /// <summary>
    ///     类名
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Template;
using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     类声明节点
/// </summary>
/// <para>示例：</para>
/// <code>
/// call(class {
///     name: utf8;
///     health: f32 = 100.0;
///     heal(mut self, amount: f32) {
///         self.health += amount;
///     }
/// })
/// </code>
/// <para>支持泛型：</para>
/// <code>
/// class Container&lt;T&gt; where T : Serializable {
///     var data: T;
/// }
/// </code>
public sealed record AnonymousClass : TermNode
{
    /// <summary>
    ///     继承规范列表（基类、接口等）
    /// </summary>
    public IReadOnlyList<InheritanceList> inheritances { get; init; } = [];

    /// <summary>
    ///     属性列表
    /// </summary>
    public IReadOnlyList<AttributeItem> attributes { get; init; } = [];

    /// <summary>
    ///     元信息声明列表
    /// </summary>
    public IReadOnlyList<FragmentArmWhenNode> metas { get; init; } = [];

    /// <summary>
    ///     修饰符列表（如 <c>public</c>、<c>abstract</c>）
    /// </summary>
    public IReadOnlyList<string> modifiers { get; init; } = [];

    /// <summary>
    ///     文档注释列表
    /// </summary>
    public IReadOnlyList<DocumentComment> doc_comments { get; init; } = [];

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
}
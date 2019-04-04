using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     Trait 声明节点，如 <c>trait Numeric { ... }</c>
///     定义一组方法签名，作为泛型约束使用
/// </summary>
public sealed record DeclareTrait : ValkyrieNode, IDeclarationNode
{
    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.trait_decl;

    /// <summary>
    ///     泛型类型参数
    /// </summary>
    public IReadOnlyList<TypeParameterList> type_parameters { get; init; } = [];

    /// <summary>
    ///     继承规范列表（父 Trait 等）
    /// </summary>
    public InheritanceList? inheritance { get; init; } = null;

    /// <summary>
    ///     泛型约束
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
    ///     Trait 名称
    /// </summary>
    public IdentifierNode? name { get; init; } = new();
}
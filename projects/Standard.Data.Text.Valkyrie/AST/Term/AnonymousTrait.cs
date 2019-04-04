using Std.Data.Text.Syntax;
using Std.Data.Text.Valkyrie.AST.Declaration;
using Std.Data.Text.Valkyrie.AST.Type;
using Std.Data.Text.Valkyrie.Parser;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     Trait 声明节点，如 <c>trait { ... }</c>
///     定义一组方法签名，作为泛型约束使用
/// </summary>
public sealed record AnonymousTrait : TermNode
{
    /// <summary>
    ///     无参构造函数
    /// </summary>
    public AnonymousTrait()
    {
    }

    /// <summary>
    ///     完整构造函数
    /// </summary>
    public AnonymousTrait(IReadOnlyList<DeclareMicro> methods, IReadOnlyList<TypeParameterList> typeParameters,
        IReadOnlyList<GenericConstraint> genericConstraints, TextSpan span)
    {
        var objectMethods = new List<DeclareObjectMethod>(methods.Count);
        foreach (var method in methods)
            objectMethods.Add(new DeclareObjectMethod
            {
                name = method.name,
                annotations = method.annotations
            });

        body = new ObjectBody
        {
            methods = objectMethods
        };
        type_parameters = typeParameters;
        generic_constraints = genericConstraints;
        this.span = span;
    }

    /// <summary>
    ///     节点类型
    /// </summary>
    public override ValkyrieNodeType type => ValkyrieNodeType.trait_decl;

    /// <summary>
    ///     泛型类型参数
    /// </summary>
    public IReadOnlyList<TypeParameterList> type_parameters { get; init; } = [];

    /// <summary>
    ///     泛型约束
    /// </summary>
    public IReadOnlyList<GenericConstraint> generic_constraints { get; init; } = [];

    /// <summary>
    ///     对象体
    /// </summary>
    public ObjectBody? body { get; init; }
}
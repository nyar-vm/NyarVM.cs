using Std.Data.Text.Valkyrie.AST.Type;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     泛型类型参数声明，如 <c>T</c>、<c>U</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro f<T>
///         { ... }
///         micro f
///         <T: Trait>
///             { ... }
///             micro f<T: Trait= default> { ... }
/// </code>
public sealed record TypeParameterItem : ValkyrieNode, IDeclarationNode
{
    public TypeParameterItem(IdentifierNode name, TypeNode? boundType, TypeNode? defaultType)
    {
        this.name = name;
        bound_type = boundType;
        default_type = defaultType;
    }

    public TypeNode? bound_type { get; }
    public TypeNode? default_type { get; }
    public Annotations annotations { get; }
    public IdentifierNode? name { get; }
}
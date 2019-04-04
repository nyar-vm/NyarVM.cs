using Std.Data.Text.Valkyrie.AST.Declaration;

namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     命名泛型实参，如 <c>Text = Self</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// f::<Type>
///         ()
///         f::
///         <[attribute] Type>
///             ()
///             f::
///             <T = Type>
///                 ()
///                 f::
///                 <T: Type>
///                     ()
///                     f::
///                     <modifier T= Type>
///                         ()
///                         f::
///                         <[attribute] T= Type>
///                             ()
///                             f::<[attribute] modifier T= Type>()
/// </code>
public sealed record TypeArgumentItem : ValkyrieNode
{
    public Annotations annotations { get; }

    /// <summary>
    ///     形参名称
    /// </summary>
    public IdentifierNode? slot { get; init; }

    /// <summary>
    ///     绑定的类型实参    ///
    /// </summary>
    public TypeNode argument { get; init; } = null!;
}
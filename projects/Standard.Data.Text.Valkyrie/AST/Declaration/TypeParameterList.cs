using System.Collections;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     泛型类型参数声明，如 <c>T</c>、<c>U</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro swap&lt;T&gt;(a: T, b: T)
/// //        ^  TypeParameter { Name = "T", Constraints = [] }
/// </code>
public sealed record TypeParameterList : ValkyrieNode, IEnumerable<TypeParameterItem>
{
    /// <summary>
    ///     关联的 <c>where</c> 约束列表
    /// </summary>
    public IReadOnlyList<TypeParameterItem> items { get; init; } = [];


    public IEnumerator<TypeParameterItem> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)items).GetEnumerator();
    }
}
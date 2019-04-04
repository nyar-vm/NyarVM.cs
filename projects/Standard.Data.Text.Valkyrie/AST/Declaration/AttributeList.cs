using System.Collections;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
/// </summary>
public sealed record AttributeList : ValkyrieNode, IEnumerable<AttributeItem>
{
    /// <summary>
    ///     属性参数列表
    /// </summary>
    public IReadOnlyList<AttributeItem> items { get; init; } = [];

    public IEnumerator<AttributeItem> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)items).GetEnumerator();
    }
}
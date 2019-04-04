using System.Collections;

namespace Std.Data.Text.Valkyrie.AST.Type;

public sealed record TypeArgumentList : ValkyrieNode, IEnumerable<TypeArgumentItem>
{
    public IReadOnlyList<TypeArgumentItem> items { get; init; } = [];

    public IEnumerator<TypeArgumentItem> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)items).GetEnumerator();
    }
}
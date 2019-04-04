using System.Collections;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     f(x, y, z)
/// </summary>
public sealed record TermArgumentList : ValkyrieNode, IEnumerable<TermArgumentItem>
{
    /// <summary>
    /// </summary>
    public IReadOnlyList<TermArgumentItem> items { get; init; } = [];

    public IEnumerator<TermArgumentItem> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
using System.Collections;

namespace Std.Data.Text.Valkyrie.AST.Declaration;

/// <summary>
///     函数参数声明
/// </summary>
/// <para>示例：</para>
/// <code>
/// micro greet(name: utf8, times: i32) { ... }
/// </code>
public sealed record TermParameterList : ValkyrieNode, IEnumerable<TermParameterItem>
{
    public IReadOnlyList<TermParameterItem> items { get; init; } = [];


    public IEnumerator<TermParameterItem> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)items).GetEnumerator();
    }
}
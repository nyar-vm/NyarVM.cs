namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     命名泛型实参，如 <c>Text = Self</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// let _: () = ();
/// let _: (i32,) = (1,);
/// let _: (i32, i64) = (1, 2);
/// let _: (ordinal: usize, value: i64) = (1, 2);
/// </code>
public sealed record TypeLiteralTupleNode : TypeNode
{
    /// <summary>
    ///     元组类型元素
    /// </summary>
    public IReadOnlyList<TypeTupleElementNode> elements { get; init; } = [];
}

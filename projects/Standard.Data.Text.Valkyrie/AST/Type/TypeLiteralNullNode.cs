namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     命名泛型实参，如 <c>Text = Self</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// let x: null = null 
/// </code>
public sealed record TypeLiteralNullNode : TypeNode
{
}
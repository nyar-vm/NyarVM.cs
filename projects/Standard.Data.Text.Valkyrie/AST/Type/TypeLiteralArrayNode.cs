namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     命名泛型实参，如 <c>Text = Self</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// let x: [f32] = [1, 2, 3, 4]
/// let x: [f32; 3] = [1, 2, 3]
/// </code>
public sealed record TypeLiteralArrayNode : TypeNode
{
}
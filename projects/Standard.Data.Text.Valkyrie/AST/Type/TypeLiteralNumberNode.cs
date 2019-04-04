namespace Std.Data.Text.Valkyrie.AST.Type;

/// <summary>
///     字面量数值类型节点，如 <c>42</c> 或 <c>[T; 8]</c> 中的定长数组大小
/// </summary>
/// <para>示例：</para>
/// <code>
/// let x: [i32; 8] = [0; 8]
/// </code>
public sealed record TypeLiteralNumberNode(int value) : TypeNode
{
}
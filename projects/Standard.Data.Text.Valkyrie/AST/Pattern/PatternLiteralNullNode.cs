namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     常量模式 —— 匹配字面常量值
/// </summary>
/// <para>示例：</para>
/// <code>
/// case null;
/// </code>
public sealed record PatternLiteralNullNode : PatternNode
{
}
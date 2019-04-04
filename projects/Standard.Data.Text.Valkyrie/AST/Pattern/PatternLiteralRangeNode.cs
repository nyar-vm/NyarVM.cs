namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     范围模式 —— 匹配闭区间 [lower, upper] 内的值。
/// </summary>
/// <para>示例：</para>
/// <code>
/// match byte {
///     case 0x00..=0x7F: Some(1),
///     case 0xC2..=0xDF: Some(2),
///     else: None
/// }
/// </code>
public sealed record PatternLiteralRangeNode : PatternNode
{
    public PatternNode? lower { get; init; }

    public PatternNode? upper { get; init; }
}

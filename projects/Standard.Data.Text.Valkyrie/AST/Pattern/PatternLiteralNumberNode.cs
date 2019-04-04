namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     常量模式 —— 匹配字面常量值
/// </summary>
/// <para>示例：</para>
/// <code>
/// case 42;
/// </code>
public sealed record PatternLiteralNumberNode : PatternNode
{
    /// <summary>
    ///     数字字面量的原始文本。
    /// </summary>
    public string value { get; init; } = "0";
}
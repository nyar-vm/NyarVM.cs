using Std.Data.Text.Valkyrie.AST;

namespace Std.Data.Text.Valkyrie.AST.Pattern;

/// <summary>
///     文本常量模式 —— 匹配字符串字面量值
/// </summary>
/// <para>示例：</para>
/// <code>
/// case "clr";
/// </code>
public sealed record PatternLiteralTextNode : PatternNode
{
    /// <summary>
    ///     文本字面量的原始内容（不含引号）。
    /// </summary>
    public string value { get; init; } = string.Empty;

    /// <summary>
    ///     文本字面量的语法类别。
    /// </summary>
    public TextLiteralKind literal_kind { get; init; } = TextLiteralKind.literal_text;
}

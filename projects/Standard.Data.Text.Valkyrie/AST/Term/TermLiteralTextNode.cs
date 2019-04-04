using Std.Data.Text.Valkyrie.AST;

namespace Std.Data.Text.Valkyrie.AST.Term;

/// <summary>
///     命名泛型实参，如 <c>Text = Self</c>
/// </summary>
/// <para>示例：</para>
/// <code>
/// let x: utf8 = "utf8" 
/// </code>
public sealed record TermLiteralTextNode : TermNode
{
    /// <summary>
    ///     字符串字面量前缀，例如 <c>r</c>、<c>re</c>。
    ///     空字符串表示普通无前缀字面量。
    /// </summary>
    public string prefix { get; init; } = string.Empty;

    /// <summary>
    ///     文本字面量的原始内容（不含引号）。
    /// </summary>
    public string value { get; init; } = string.Empty;

    /// <summary>
    ///     文本字面量的语法类别。
    ///     `''` 对应 `literal_char`，`""` 对应 `literal_text`。
    /// </summary>
    public TextLiteralKind literal_kind { get; init; } = TextLiteralKind.literal_text;
}

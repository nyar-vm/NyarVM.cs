namespace Std.Data.Text.Valkyrie.AST;

/// <summary>
///     文本字面量的语法类别。
/// </summary>
public enum TextLiteralKind : byte
{
    /// <summary>
    ///     双引号文本字面量，例如 `"hello"`。
    /// </summary>
    literal_text,

    /// <summary>
    ///     单引号字符字面量，例如 `'x'`。
    /// </summary>
    literal_char
}

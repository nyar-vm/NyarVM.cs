namespace Std.Data.Text.Prolog.Lexer;

public static class PlOperators
{
    public static readonly HashSet<string> All =
    [
        new(StringComparer.Ordinal),
        ":-", "?-", "-->", "=", "\\=", "==", "\\==", "@<", "@>",
        "@=<", "@>=", "is", "=..", "\\+", "<", ">", "=<", ">=",
        "+", "-", "*", "/", "//", "mod", "rem", "**", "^",
        "<<", ">>", "/\\", "\\/", "\\", "and", "or", "not",
        "->", ";", ",", "|", "!", "\\"
    ];
}

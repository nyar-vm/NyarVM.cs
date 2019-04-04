namespace Std.Data.Text.Julia.Lexer;

public static class JlKeywords
{
    public static readonly HashSet<string> all =
    [
        new(StringComparer.Ordinal),
        "begin", "while", "for", "in", "if", "else", "elseif", "end",
        "function", "macro", "return", "try", "catch", "finally",
        "struct", "mutable", "abstract", "type", "module", "import",
        "export", "using", "do", "let", "const", "global", "local",
        "true", "false", "nothing", "where", "isa", "new",
        "break", "continue", "quote"
    ];
}

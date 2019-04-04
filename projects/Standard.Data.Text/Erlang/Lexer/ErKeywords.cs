namespace Std.Data.Text.Erlang.Lexer;

public static class ErKeywords
{
    public static readonly HashSet<string> All =
    [
        new(StringComparer.Ordinal),
        "after", "begin", "case", "catch", "cond", "end", "fun",
        "if", "let", "of", "receive", "try", "when", "andalso",
        "orelse", "not", "and", "or", "band", "bor", "bxor",
        "bnot", "bsl", "bsr", "div", "rem", "xor",
        "query", "maybe", "else"
    ];
}

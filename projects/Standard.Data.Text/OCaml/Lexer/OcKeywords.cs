namespace Std.Data.Text.OCaml.Lexer;

public static class OcKeywords
{
    public static readonly HashSet<string> All =
    [
        new(StringComparer.Ordinal),
        "let", "rec", "in", "if", "then", "else", "match", "with",
        "fun", "function", "type", "of", "module", "struct", "end",
        "sig", "open", "include", "begin", "try", "raise", "exception",
        "val", "mutable", "ref", "for", "to", "downto", "do", "done",
        "while", "assert", "when", "as", "constraint", "external",
        "false", "true", "not", "mod", "land", "lor", "lxor", "lsl",
        "lsr", "asr", "private", "virtual", "object", "method",
        "inherit", "initializer", "new", "class", "and", "or",
        "effect", "handle", "perform"
    ];
}

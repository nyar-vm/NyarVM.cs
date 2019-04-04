namespace Std.Data.Text.Haskell.Lexer;

public static class HsKeywords
{
    public static readonly HashSet<string> All =
    [
        new(StringComparer.Ordinal),
        "module", "where", "import", "data", "type", "class", "instance",
        "deriving", "if", "then", "else", "case", "of", "let", "in",
        "do", "return", "infixl", "infixr", "infix", "newtype",
        "qualified", "as", "hiding", "forall", "family", "where",
        "pattern", "default", "foreign", "export", "safe", "unsafe",
        "interruptible", "threadsafe", "stdcall", "ccall", "cplusplus",
        "dotnet", "jvm", "js", "javascript", "capi", "prim"
    ];
}

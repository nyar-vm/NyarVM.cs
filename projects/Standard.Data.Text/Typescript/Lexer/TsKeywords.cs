namespace Std.Data.Text.Typescript.Lexer;

public static class TsKeywords
{
    public static readonly HashSet<string> all =
    [
        new(StringComparer.Ordinal),
        "abstract", "any", "as", "asserts", "async", "await",
        "bigint", "boolean", "break",
        "case", "catch", "class", "const", "constructor", "continue",
        "debugger", "declare", "default", "delete", "do",
        "else", "enum", "export", "extends",
        "false", "finally", "for", "from", "function",
        "get", "global",
        "if", "implements", "import", "in", "infer", "instanceof", "interface",
        "keyof",
        "let",
        "module",
        "namespace", "never", "new", "null",
        "object", "of",
        "package", "private", "protected", "public",
        "readonly", "require", "return",
        "set", "static", "string", "super", "switch", "symbol",
        "this", "throw", "true", "try", "type", "typeof",
        "undefined", "unique", "unknown",
        "var", "void",
        "while", "with",
        "yield"
    ];
}


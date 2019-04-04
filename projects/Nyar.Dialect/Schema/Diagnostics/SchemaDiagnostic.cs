namespace Nyar.Dialect.Schema.Diagnostics;

public enum SchemaDiagnosticLevel
{
    error,
    warning,
    info
}

public sealed class SchemaDiagnostic
{
    public SchemaDiagnostic(SchemaDiagnosticLevel level, string code, string message, string? filePath = null,
        int line = 0, int column = 0)
    {
        this.level = level;
        this.code = code;
        this.message = message;
        file_path = filePath;
        this.line = line;
        this.column = column;
    }

    public SchemaDiagnosticLevel level { get; }
    public string code { get; }
    public string message { get; }
    public string? file_path { get; }
    public int line { get; }
    public int column { get; }

    public override string ToString()
    {
        var levelStr = level.ToString().ToLowerInvariant();
        if (file_path is not null) return $"[{levelStr} {code}] {file_path}:{line}:{column} - {message}";
        return $"[{levelStr} {code}] {message}";
    }
}
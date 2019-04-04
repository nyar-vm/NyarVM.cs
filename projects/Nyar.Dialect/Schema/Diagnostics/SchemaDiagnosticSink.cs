namespace Nyar.Dialect.Schema.Diagnostics;

public sealed class SchemaDiagnosticSink
{
    private readonly List<SchemaDiagnostic> _diagnostics = [];

    public IReadOnlyList<SchemaDiagnostic> diagnostics => _diagnostics;

    public bool has_errors => _diagnostics.Any(d => d.level == SchemaDiagnosticLevel.error);

    public void add_error(string? filePath, int line, int column, string code, string message)
    {
        _diagnostics.Add(new SchemaDiagnostic(SchemaDiagnosticLevel.error, code, message, filePath, line, column));
    }

    public void add_warning(string? filePath, int line, int column, string code, string message)
    {
        _diagnostics.Add(new SchemaDiagnostic(SchemaDiagnosticLevel.warning, code, message, filePath, line, column));
    }

    public void add_info(string? filePath, int line, int column, string code, string message)
    {
        _diagnostics.Add(new SchemaDiagnostic(SchemaDiagnosticLevel.info, code, message, filePath, line, column));
    }

    public void clear()
    {
        _diagnostics.Clear();
    }
}
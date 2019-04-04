namespace Std.Data.Text.Diagnostics;

public enum DiagnosticSeverity
{
    fatal,
    error,
    warning,
    info,
    hint,
    debug,
    trace
}

public static class DiagnosticSeverityExtensions
{
    public static bool is_error_level(this DiagnosticSeverity severity)
    {
        return severity is DiagnosticSeverity.fatal or DiagnosticSeverity.error;
    }

    public static bool is_warning_level(this DiagnosticSeverity severity)
    {
        return severity == DiagnosticSeverity.warning;
    }
}

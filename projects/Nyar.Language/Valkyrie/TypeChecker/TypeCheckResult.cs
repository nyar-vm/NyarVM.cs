using Std.Data.Text.Diagnostics;

namespace Nyar.Language.Valkyrie.TypeChecker;

public sealed class TypeCheckResult
{
    public TypeCheckResult(IReadOnlyList<TypeDiagnostic> diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    public IReadOnlyList<TypeDiagnostic> diagnostics { get; }
    public bool has_errors => diagnostics.Any(d => d.severity.is_error_level());
    public bool has_warnings => diagnostics.Any(d => d.severity.is_warning_level());
}

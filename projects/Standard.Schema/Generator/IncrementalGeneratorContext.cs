using Hermes.Migration;

namespace Hermes.Generator;

public sealed class IncrementalGeneratorContext
{
    public SchemaIR Schema { get; init; } = null!;
    public SchemaIR PreviousSchema { get; init; } = null!;
    public SchemaDiff Diff { get; init; } = null!;
    public ISet<string> ChangedTypeNames { get; init; } = new HashSet<string>();
    public ISet<string> RemovedTypeNames { get; init; } = new HashSet<string>();
    public string SchemaPath { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public IReadOnlyDictionary<string, object> Options { get; init; } = new Dictionary<string, object>();
    public SchemaDiagnosticSink Diagnostics { get; init; } = new();
}
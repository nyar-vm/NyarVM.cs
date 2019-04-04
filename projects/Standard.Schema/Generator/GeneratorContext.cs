namespace Hermes.Generator;

public sealed class GeneratorContext
{
    public SchemaIR Schema { get; init; } = null!;
    public string SchemaPath { get; init; } = "";
    public string OutputPath { get; init; } = "";
    public IReadOnlyDictionary<string, object> Options { get; init; } = new Dictionary<string, object>();
    public SchemaDiagnosticSink Diagnostics { get; init; } = new();
}
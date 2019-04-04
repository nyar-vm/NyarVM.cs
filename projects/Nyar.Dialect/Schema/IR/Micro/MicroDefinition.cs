using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;
using Nyar.Dialect.Schema.IR.Where;

namespace Nyar.Dialect.Schema.IR.Micro;

public sealed class MicroDefinition
{
    public MicroDefinition(string name, IReadOnlyList<ParameterDefinition> parameters, SchemaType? returnType,
        string? body = null, IReadOnlyList<AttributeDefinition>? attributes = null, int sourceLine = 0,
        int sourceColumn = 0)
    {
        this.name = name;
        this.parameters = parameters;
        return_type = returnType;
        this.body = body;
        this.attributes = attributes ?? [];
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public IReadOnlyList<ParameterDefinition> parameters { get; }
    public SchemaType? return_type { get; }
    public string? body { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public int source_line { get; }
    public int source_column { get; }
}
using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.Where;

public sealed class ParameterDefinition
{
    public ParameterDefinition(string name, SchemaType parameterType,
        IReadOnlyList<AttributeDefinition>? attributes = null, int sourceLine = 0, int sourceColumn = 0)
    {
        this.name = name;
        parameter_type = parameterType;
        this.attributes = attributes ?? [];
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public SchemaType parameter_type { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public int source_line { get; }
    public int source_column { get; }
}
using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.How;

public sealed class StreamDefinition
{
    public StreamDefinition(string name, SchemaType eventType, SchemaType? keyType = null,
        IReadOnlyList<AttributeDefinition>? attributes = null, int sourceLine = 0, int sourceColumn = 0)
    {
        this.name = name;
        event_type = eventType;
        key_type = keyType;
        this.attributes = attributes ?? [];
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public SchemaType event_type { get; }
    public SchemaType? key_type { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public int source_line { get; }
    public int source_column { get; }
}
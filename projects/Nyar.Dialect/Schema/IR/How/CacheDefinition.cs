using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.How;

public sealed class CacheDefinition
{
    public CacheDefinition(string name, SchemaType keyType, SchemaType valueType,
        IReadOnlyList<AttributeDefinition>? attributes = null, int sourceLine = 0, int sourceColumn = 0)
    {
        this.name = name;
        key_type = keyType;
        value_type = valueType;
        this.attributes = attributes ?? [];
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public SchemaType key_type { get; }
    public SchemaType value_type { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public int source_line { get; }
    public int source_column { get; }
}
using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;

namespace Nyar.Dialect.Schema.IR.What;

public sealed class UnionVariant
{
    public UnionVariant(string name, SchemaType? payload = null, IReadOnlyList<AttributeDefinition>? attributes = null)
    {
        this.name = name;
        this.payload = payload;
        this.attributes = attributes ?? [];
    }

    public string name { get; }
    public SchemaType? payload { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
}
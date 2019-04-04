using Nyar.Dialect.Schema.IR.Common;
using Nyar.Dialect.Schema.IR.Types;
using Nyar.Dialect.Schema.IR.What;

namespace Nyar.Dialect.Schema.IR.How;

public sealed class ModelDefinition
{
    public ModelDefinition(string name, SchemaType keyType, IReadOnlyList<FieldDefinition> fields,
        IReadOnlyList<GetterDefinition>? getters = null, IReadOnlyList<AttributeDefinition>? attributes = null,
        string? docComment = null, int sourceLine = 0, int sourceColumn = 0)
    {
        this.name = name;
        key_type = keyType;
        this.fields = fields;
        this.getters = getters ?? [];
        this.attributes = attributes ?? [];
        doc_comment = docComment;
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public SchemaType key_type { get; }
    public IReadOnlyList<FieldDefinition> fields { get; }
    public IReadOnlyList<GetterDefinition> getters { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public string? doc_comment { get; }
    public int source_line { get; }
    public int source_column { get; }
}
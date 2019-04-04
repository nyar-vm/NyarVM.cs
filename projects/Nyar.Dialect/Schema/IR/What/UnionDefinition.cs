using Nyar.Dialect.Schema.IR.Common;

namespace Nyar.Dialect.Schema.IR.What;

public sealed class UnionDefinition
{
    public UnionDefinition(string name, IReadOnlyList<UnionVariant> variants,
        IReadOnlyList<AttributeDefinition>? attributes = null, string? sourceFile = null, int sourceLine = 0,
        int sourceColumn = 0)
    {
        this.name = name;
        this.variants = variants;
        this.attributes = attributes ?? [];
        source_file = sourceFile;
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public IReadOnlyList<UnionVariant> variants { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public string? source_file { get; }
    public int source_line { get; }
    public int source_column { get; }
}
using Nyar.Dialect.Schema.IR.Common;

namespace Nyar.Dialect.Schema.IR.What;

public sealed class BaseClass
{
    public BaseClass(string name)
    {
        this.name = name;
    }

    public string name { get; }
}

public sealed class ClassDefinition
{
    public ClassDefinition(string name, IReadOnlyList<FieldDefinition> fields,
        IReadOnlyList<AttributeDefinition> attributes, BaseClass? baseClass = null, string? sourceFile = null,
        int sourceLine = 0, int sourceColumn = 0)
    {
        this.name = name;
        this.fields = fields;
        this.attributes = attributes;
        @base = baseClass;
        source_file = sourceFile;
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public IReadOnlyList<FieldDefinition> fields { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public BaseClass? @base { get; }
    public string? source_file { get; }
    public int source_line { get; }
    public int source_column { get; }
}
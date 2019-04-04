using Nyar.Dialect.Schema.IR.Common;

namespace Nyar.Dialect.Schema.IR.What;

public sealed class FlagsDefinition
{
    public FlagsDefinition(string name, IReadOnlyList<EnumMember> members,
        IReadOnlyList<AttributeDefinition>? attributes = null, string? sourceFile = null, int sourceLine = 0,
        int sourceColumn = 0)
    {
        this.name = name;
        this.members = members;
        this.attributes = attributes ?? [];
        source_file = sourceFile;
        source_line = sourceLine;
        source_column = sourceColumn;
    }

    public string name { get; }
    public IReadOnlyList<EnumMember> members { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
    public string? source_file { get; }
    public int source_line { get; }
    public int source_column { get; }
}
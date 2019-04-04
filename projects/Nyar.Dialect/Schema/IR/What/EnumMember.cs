using Nyar.Dialect.Schema.IR.Common;

namespace Nyar.Dialect.Schema.IR.What;

public sealed class EnumMember
{
    public EnumMember(string name, int value, IReadOnlyList<AttributeDefinition>? attributes = null)
    {
        this.name = name;
        this.value = value;
        this.attributes = attributes ?? [];
    }

    public string name { get; }
    public int value { get; }
    public IReadOnlyList<AttributeDefinition> attributes { get; }
}
namespace Nyar.Dialect.Schema.IR.Common;

public sealed class AttributeDefinition
{
    public AttributeDefinition(string name, IReadOnlyList<KeyValuePair<string, string>>? arguments = null)
    {
        this.name = name;
        this.arguments = arguments ?? [];
    }

    public string name { get; }
    public IReadOnlyList<KeyValuePair<string, string>> arguments { get; }
}
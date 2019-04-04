namespace Nyar.Dialect.Schema.IR.Types;

public sealed class NamedType : SchemaType
{
    public NamedType(string name, string? @namespace = null)
    {
        this.name = name;
        this.@namespace = @namespace;
    }

    public string name { get; }
    public string? @namespace { get; }

    public override string type_name => @namespace is null
        ? name
        : $"{@namespace}.{name}";
}
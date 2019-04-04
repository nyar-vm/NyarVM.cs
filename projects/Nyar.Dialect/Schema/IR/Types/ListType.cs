namespace Nyar.Dialect.Schema.IR.Types;

public sealed class ListType : SchemaType
{
    public ListType(SchemaType elementType)
    {
        element_type = elementType;
    }

    public SchemaType element_type { get; }

    public override string type_name => $"[{element_type.type_name}]";
}
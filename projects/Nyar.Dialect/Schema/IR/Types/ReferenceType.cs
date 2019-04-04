namespace Nyar.Dialect.Schema.IR.Types;

public sealed class ReferenceType : SchemaType
{
    public ReferenceType(SchemaType referencedType)
    {
        referenced_type = referencedType;
    }

    public SchemaType referenced_type { get; }

    public override string type_name => $"&{referenced_type.type_name}";
}
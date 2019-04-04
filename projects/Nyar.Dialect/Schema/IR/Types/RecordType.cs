namespace Nyar.Dialect.Schema.IR.Types;

public sealed class RecordType : SchemaType
{
    public RecordType(SchemaType keyType, SchemaType valueType)
    {
        key_type = keyType;
        value_type = valueType;
    }

    public SchemaType key_type { get; }
    public SchemaType value_type { get; }

    public override string type_name => $"structure<{key_type.type_name}, {value_type.type_name}>";
}
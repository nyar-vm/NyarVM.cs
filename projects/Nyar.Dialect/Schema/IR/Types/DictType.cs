namespace Nyar.Dialect.Schema.IR.Types;

public sealed class DictType : SchemaType
{
    public DictType(SchemaType keyType, SchemaType valueType)
    {
        key_type = keyType;
        value_type = valueType;
    }

    public SchemaType key_type { get; }
    public SchemaType value_type { get; }

    public override string type_name => $"dict<{key_type.type_name}, {value_type.type_name}>";
}
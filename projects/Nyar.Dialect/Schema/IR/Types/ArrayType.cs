namespace Nyar.Dialect.Schema.IR.Types;

public sealed class ArrayType : SchemaType
{
    public ArrayType(SchemaType elementType, int size)
    {
        element_type = elementType;
        this.size = size;
    }

    public SchemaType element_type { get; }
    public int size { get; }

    public override string type_name => $"[{element_type.type_name}; {size}]";
}
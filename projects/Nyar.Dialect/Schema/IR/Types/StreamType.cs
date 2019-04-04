namespace Nyar.Dialect.Schema.IR.Types;

public sealed class StreamType : SchemaType
{
    public StreamType(SchemaType innerType)
    {
        inner_type = innerType;
    }

    public SchemaType inner_type { get; }

    public override string type_name => $"stream<{inner_type.type_name}>";
}
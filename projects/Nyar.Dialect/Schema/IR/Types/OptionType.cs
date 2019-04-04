namespace Nyar.Dialect.Schema.IR.Types;

public sealed class OptionType : SchemaType
{
    public OptionType(SchemaType innerType)
    {
        inner_type = innerType;
    }

    public SchemaType inner_type { get; }

    public override string type_name => $"option<{inner_type.type_name}>";
}
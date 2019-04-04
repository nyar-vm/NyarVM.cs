namespace Nyar.Dialect.Schema.IR.Types;

public sealed class PrimitiveType : SchemaType
{
    public static readonly PrimitiveType i8 = new("i8");
    public static readonly PrimitiveType i16 = new("i16");
    public static readonly PrimitiveType i32 = new("i32");
    public static readonly PrimitiveType i64 = new("i64");
    public static readonly PrimitiveType u8 = new("u8");
    public static readonly PrimitiveType u16 = new("u16");
    public static readonly PrimitiveType u32 = new("u32");
    public static readonly PrimitiveType u64 = new("u64");
    public static readonly PrimitiveType f32 = new("f32");
    public static readonly PrimitiveType f64 = new("f64");
    public static readonly PrimitiveType @bool = new("bool");
    public static readonly PrimitiveType utf8 = new("utf8");
    public static readonly PrimitiveType utf16 = new("utf16");
    public static readonly PrimitiveType uuid = new("uuid");
    public static readonly PrimitiveType unit = new("unit");
    public static readonly PrimitiveType @object = new("object");
    public static readonly PrimitiveType date_time = new("datetime");
    public static readonly PrimitiveType @decimal = new("decimal");

    private PrimitiveType(string typeName)
    {
        type_name = typeName;
    }

    public override string type_name { get; }

    public static PrimitiveType? from_name(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "i8" => i8, "i16" => i16, "i32" => i32, "i64" => i64,
            "u8" => u8, "u16" => u16, "u32" => u32, "u64" => u64,
            "f32" => f32, "f64" => f64,
            "bool" => @bool,
            "utf8" => utf8, "utf16" => utf16, "uuid" => uuid, "unit" => unit, "object" => @object,
            "datetime" => date_time,
            "decimal" => @decimal,
            _ => null
        };
    }
}
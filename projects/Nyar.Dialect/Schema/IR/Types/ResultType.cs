namespace Nyar.Dialect.Schema.IR.Types;

public sealed class ResultType : SchemaType
{
    public ResultType(SchemaType okType, SchemaType? errorType = null)
    {
        ok_type = okType;
        error_type = errorType;
    }

    public SchemaType ok_type { get; }
    public SchemaType? error_type { get; }

    public override string type_name => error_type is null
        ? $"result<{ok_type.type_name}>"
        : $"result<{ok_type.type_name}, {error_type.type_name}>";
}
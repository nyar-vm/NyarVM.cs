namespace Std.Data.Text.Sql;

public sealed class LiteralValue : SqlExpression
{
    public LiteralValue(object? value, SqlTokenType type)
    {
        this.value = value;
        this.type = type;
    }

    public object? value { get; }
    public SqlTokenType type { get; }

    public override string ToString()
    {
        return type switch
        {
            SqlTokenType.@string => $"'{value}'",
            SqlTokenType.@true => "TRUE",
            SqlTokenType.@false => "FALSE",
            SqlTokenType.@null => "NULL",
            _ => value?.ToString() ?? "NULL"
        };
    }
}
namespace Std.Data.Text.Sql;

public sealed class StarExpression : SqlExpression
{
    public StarExpression(string? table = null)
    {
        this.table = table;
    }

    public string? table { get; }

    public override string ToString()
    {
        return table is not null ? $"{table}.*" : "*";
    }
}
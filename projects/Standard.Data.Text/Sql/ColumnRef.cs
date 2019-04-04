namespace Std.Data.Text.Sql;

public sealed class ColumnRef : SqlExpression
{
    public ColumnRef(string name, string? table = null)
    {
        this.table = table;
        this.name = name;
    }

    public string? table { get; }
    public string name { get; }

    public override string ToString()
    {
        return table is not null ? $"{table}.{name}" : name;
    }
}
namespace Std.Data.Text.Sql;

public sealed class DeleteStatement : SqlNode
{
    public DeleteStatement(string table, SqlExpression? where = null, IReadOnlyList<SqlColumn>? returning = null)
    {
        this.table = table;
        this.where = where;
        this.returning = returning;
    }

    public string table { get; }
    public SqlExpression? where { get; }
    public IReadOnlyList<SqlColumn>? returning { get; }

    public override string ToString()
    {
        var parts = new List<string> { $"DELETE FROM {table}" };
        if (where is not null) parts.Add($"WHERE {where}");

        return string.Join(" ", parts);
    }
}
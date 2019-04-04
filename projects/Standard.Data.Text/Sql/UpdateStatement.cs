namespace Std.Data.Text.Sql;

public sealed class UpdateStatement : SqlNode
{
    public UpdateStatement(
        string table,
        IReadOnlyList<(string Column, SqlExpression Value)> assignments,
        SqlExpression? where = null,
        IReadOnlyList<SqlColumn>? returning = null)
    {
        this.table = table;
        this.assignments = assignments;
        this.where = where;
        this.returning = returning;
    }

    public string table { get; }
    public IReadOnlyList<(string Column, SqlExpression Value)> assignments { get; }
    public SqlExpression? where { get; }
    public IReadOnlyList<SqlColumn>? returning { get; }

    public override string ToString()
    {
        var sets = string.Join(", ", assignments.Select(a => $"{a.Column} = {a.Value}"));
        var parts = new List<string> { $"UPDATE {table} SET {sets}" };
        if (where is not null) parts.Add($"WHERE {where}");

        return string.Join(" ", parts);
    }
}
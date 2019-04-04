namespace Std.Data.Text.Sql;

public sealed class SelectStatement : SqlNode
{
    public SelectStatement(
        IReadOnlyList<SqlColumn> columns,
        SqlTableRef? from = null,
        SqlExpression? where = null,
        IReadOnlyList<SqlTableRef>? joins = null,
        IReadOnlyList<SqlExpression>? groupBy = null,
        SqlExpression? having = null,
        IReadOnlyList<OrderByItem>? orderBy = null,
        int? limit = null,
        int? offset = null,
        bool distinct = false)
    {
        this.columns = columns;
        this.from = from;
        this.where = where;
        this.joins = joins ?? [];
        group_by = groupBy ?? [];
        this.having = having;
        order_by = orderBy ?? [];
        this.limit = limit;
        this.offset = offset;
        this.distinct = distinct;
    }

    public IReadOnlyList<SqlColumn> columns { get; }
    public SqlTableRef? from { get; }
    public SqlExpression? where { get; }
    public IReadOnlyList<SqlTableRef> joins { get; }
    public IReadOnlyList<SqlExpression> group_by { get; }
    public SqlExpression? having { get; }
    public IReadOnlyList<OrderByItem> order_by { get; }
    public int? limit { get; }
    public int? offset { get; }
    public bool distinct { get; }

    public override string ToString()
    {
        var distinct = this.distinct ? "DISTINCT " : "";
        var parts = new List<string> { $"SELECT {distinct}{string.Join(", ", columns)}" };
        if (from is not null) parts.Add($"FROM {from}");

        if (where is not null) parts.Add($"WHERE {where}");

        return string.Join(" ", parts);
    }
}
namespace Std.Data.Text.Sql;

/// <summary>
///     EXISTS (子查询) 表达式
///     。
/// </summary>
public sealed class ExistsExpr : SqlExpression
{
    public ExistsExpr(SelectStatement subquery, bool negated = false)
    {
        this.subquery = subquery;
        this.negated = negated;
    }

    public SelectStatement subquery { get; }
    public bool negated { get; }

    public override string ToString()
    {
        var not = negated ? "NOT " : "";
        return $"{not}EXISTS ({subquery})";
    }
}
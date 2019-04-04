namespace Std.Data.Text.Sql;

/// <summary>
///     IN (子查询) 表达式
///     。
/// </summary>
public sealed class InSubqueryExpr : SqlExpression
{
    public InSubqueryExpr(SqlExpression expression, SelectStatement subquery, bool negated = false)
    {
        this.expression = expression;
        this.subquery = subquery;
        this.negated = negated;
    }

    public SqlExpression expression { get; }
    public SelectStatement subquery { get; }
    public bool negated { get; }

    public override string ToString()
    {
        var not = negated ? "NOT " : "";
        return $"{expression} {not}IN ({subquery})";
    }
}
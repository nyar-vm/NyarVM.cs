namespace Std.Data.Text.Sql;

/// <summary>
///     子查询表达式：(SELECT ...)
///     。
/// </summary>
public sealed class SubqueryExpr : SqlExpression
{
    public SubqueryExpr(SelectStatement query)
    {
        this.query = query;
    }

    public SelectStatement query { get; }

    public override string ToString()
    {
        return $"({query})";
    }
}
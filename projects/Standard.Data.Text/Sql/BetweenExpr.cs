namespace Std.Data.Text.Sql;

public sealed class BetweenExpr : SqlExpression
{
    public BetweenExpr(SqlExpression expression, SqlExpression low, SqlExpression high, bool negated = false)
    {
        this.expression = expression;
        this.low = low;
        this.high = high;
        this.negated = negated;
    }

    public SqlExpression expression { get; }
    public SqlExpression low { get; }
    public SqlExpression high { get; }
    public bool negated { get; }

    public override string ToString()
    {
        var not = negated ? "NOT " : "";
        return $"{expression} {not}BETWEEN {low} AND {high}";
    }
}
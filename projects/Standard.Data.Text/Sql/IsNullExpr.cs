namespace Std.Data.Text.Sql;

public sealed class IsNullExpr : SqlExpression
{
    public IsNullExpr(SqlExpression expression, bool negated = false)
    {
        this.expression = expression;
        this.negated = negated;
    }

    public SqlExpression expression { get; }
    public bool negated { get; }

    public override string ToString()
    {
        var not = negated ? "NOT " : "";
        return $"{expression} IS {not}NULL";
    }
}
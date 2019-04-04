namespace Std.Data.Text.Sql;

public sealed class InExpr : SqlExpression
{
    public InExpr(SqlExpression expression, IReadOnlyList<SqlExpression> values, bool negated = false)
    {
        this.expression = expression;
        this.values = values;
        this.negated = negated;
    }

    public SqlExpression expression { get; }
    public IReadOnlyList<SqlExpression> values { get; }
    public bool negated { get; }

    public override string ToString()
    {
        var not = negated ? "NOT " : "";
        return $"{expression} {not}IN ({string.Join(", ", values)})";
    }
}
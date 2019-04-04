namespace Std.Data.Text.Sql;

public sealed class UnaryExpr : SqlExpression
{
    public UnaryExpr(string op, SqlExpression operand)
    {
        @operator = op;
        this.operand = operand;
    }

    public string @operator { get; }
    public SqlExpression operand { get; }

    public override string ToString()
    {
        return $"({@operator} {operand})";
    }
}
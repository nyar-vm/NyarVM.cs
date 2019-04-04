namespace Std.Data.Text.Sql;

public sealed class BinaryExpr : SqlExpression
{
    public BinaryExpr(SqlExpression left, string op, SqlExpression right)
    {
        this.left = left;
        @operator = op;
        this.right = right;
    }

    public SqlExpression left { get; }
    public string @operator { get; }
    public SqlExpression right { get; }

    public override string ToString()
    {
        return $"({left} {@operator} {right})";
    }
}
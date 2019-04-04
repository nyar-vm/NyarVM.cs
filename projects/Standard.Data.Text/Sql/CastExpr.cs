namespace Std.Data.Text.Sql;

/// <summary>
///     CAST(expr AS type) 表达式
///     。
/// </summary>
public sealed class CastExpr : SqlExpression
{
    public CastExpr(SqlExpression expression, string targetType)
    {
        this.expression = expression;
        target_type = targetType;
    }

    public SqlExpression expression { get; }
    public string target_type { get; }

    public override string ToString()
    {
        return $"CAST({expression} AS {target_type})";
    }
}
namespace Std.Data.Text.Sql;

/// <summary>
///     expr COLLATE collation 表达式
///     。
/// </summary>
public sealed class CollateExpr : SqlExpression
{
    public CollateExpr(SqlExpression expression, string collation)
    {
        this.expression = expression;
        this.collation = collation;
    }

    public SqlExpression expression { get; }
    public string collation { get; }

    public override string ToString()
    {
        return $"{expression} COLLATE {collation}";
    }
}
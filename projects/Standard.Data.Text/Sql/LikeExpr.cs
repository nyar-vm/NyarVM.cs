namespace Std.Data.Text.Sql;

/// <summary>
///     LIKE / ILIKE 表达式
///     。
/// </summary>
public sealed class LikeExpr : SqlExpression
{
    public LikeExpr(SqlExpression expression, SqlExpression pattern, bool negated = false, bool caseInsensitive = false)
    {
        this.expression = expression;
        this.pattern = pattern;
        this.negated = negated;
        case_insensitive = caseInsensitive;
    }

    public SqlExpression expression { get; }
    public SqlExpression pattern { get; }
    public bool negated { get; }
    public bool case_insensitive { get; }

    public override string ToString()
    {
        var op = case_insensitive ? "ILIKE" : negated ? "NOT LIKE" : "LIKE";
        return $"{expression} {op} {pattern}";
    }
}
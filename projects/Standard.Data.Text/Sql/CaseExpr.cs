namespace Std.Data.Text.Sql;

/// <summary>
///     CASE WHEN ... THEN ... ELSE ... END 表达式
///     。
/// </summary>
public sealed class CaseExpr : SqlExpression
{
    public CaseExpr(
        IReadOnlyList<(SqlExpression When, SqlExpression Then)> whenClauses,
        SqlExpression? elseExpr = null)
    {
        when_clauses = whenClauses;
        else_expr = elseExpr;
    }

    public IReadOnlyList<(SqlExpression When, SqlExpression Then)> when_clauses { get; }
    public SqlExpression? else_expr { get; }

    public override string ToString()
    {
        var parts = new List<string> { "CASE" };
        foreach (var (w, t) in when_clauses) parts.Add($"WHEN {w} THEN {t}");

        if (else_expr is not null) parts.Add($"ELSE {else_expr}");

        parts.Add("END");
        return string.Join(" ", parts);
    }
}
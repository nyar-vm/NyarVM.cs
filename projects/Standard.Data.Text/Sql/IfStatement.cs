namespace Std.Data.Text.Sql;

/// <summary>
///     IF 语句（存储过程内使用）
///     。
/// </summary>
public sealed class IfStatement : SqlNode
{
    public IfStatement(
        SqlExpression condition,
        IReadOnlyList<SqlNode> thenBody,
        IReadOnlyList<ElseIfClause> elseIfClauses,
        IReadOnlyList<SqlNode>? elseBody)
    {
        this.condition = condition;
        then_body = thenBody;
        else_if_clauses = elseIfClauses;
        else_body = elseBody;
    }

    public SqlExpression condition { get; }
    public IReadOnlyList<SqlNode> then_body { get; }
    public IReadOnlyList<ElseIfClause> else_if_clauses { get; }
    public IReadOnlyList<SqlNode>? else_body { get; }

    public override string ToString()
    {
        return $"IF ({condition}) THEN ... END IF";
    }
}
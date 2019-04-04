namespace Std.Data.Text.Sql;

public sealed class ElseIfClause : SqlNode
{
    public ElseIfClause(SqlExpression condition, IReadOnlyList<SqlNode> body)
    {
        this.condition = condition;
        this.body = body;
    }

    public SqlExpression condition { get; }
    public IReadOnlyList<SqlNode> body { get; }

    public override string ToString()
    {
        return $"ELSIF ({condition}) THEN ...";
    }
}
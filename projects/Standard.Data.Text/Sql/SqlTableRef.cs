namespace Std.Data.Text.Sql;

public sealed class SqlTableRef : SqlNode
{
    public SqlTableRef(string name, string? alias = null, SqlTokenType joinType = SqlTokenType.from,
        SqlExpression? onCondition = null)
    {
        this.name = name;
        this.alias = alias;
        join_type = joinType;
        on_condition = onCondition;
    }

    public string name { get; }
    public string? alias { get; }
    public SqlTokenType join_type { get; }
    public SqlExpression? on_condition { get; }

    public override string ToString()
    {
        return alias is not null ? $"{name} AS {alias}" : name;
    }
}
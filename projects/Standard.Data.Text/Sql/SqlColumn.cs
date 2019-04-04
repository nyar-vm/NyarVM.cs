namespace Std.Data.Text.Sql;

public sealed class SqlColumn : SqlNode
{
    public SqlColumn(SqlExpression expression, string? alias = null)
    {
        this.expression = expression;
        this.alias = alias;
    }

    public SqlExpression expression { get; }
    public string? alias { get; }

    public override string ToString()
    {
        return alias is not null ? $"{expression} AS {alias}" : expression.ToString();
    }
}
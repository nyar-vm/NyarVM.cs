namespace Std.Data.Text.Sql;

/// <summary>
///     EXECUTE 语句 — 执行预处理语句
///     EXECUTE stmt_name [USING @var1, @var2, ...]
///     。
/// </summary>
public sealed class ExecuteStatement : SqlNode
{
    public ExecuteStatement(string name, IReadOnlyList<SqlExpression> parameters)
    {
        this.name = name;
        this.parameters = parameters;
    }

    public string name { get; }
    public IReadOnlyList<SqlExpression> parameters { get; }

    public override string ToString()
    {
        return $"EXECUTE {name}";
    }
}
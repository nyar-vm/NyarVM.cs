namespace Std.Data.Text.Sql;

/// <summary>
///     ALTER TABLE 语句
///     。
/// </summary>
public sealed class AlterTableStatement : SqlNode
{
    public AlterTableStatement(string table, AlterTableAction action)
    {
        this.table = table;
        this.action = action;
    }

    public string table { get; }
    public AlterTableAction action { get; }

    public override string ToString()
    {
        return $"ALTER TABLE {table} {action}";
    }
}
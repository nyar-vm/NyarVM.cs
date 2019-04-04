namespace Std.Data.Text.Sql;

/// <summary>
///     ALTER TABLE DROP COLUMN 操作
///     。
/// </summary>
public sealed class DropColumnAction : AlterTableAction
{
    public DropColumnAction(string columnName)
    {
        column_name = columnName;
    }

    public string column_name { get; }

    public override string ToString()
    {
        return $"DROP COLUMN {column_name}";
    }
}
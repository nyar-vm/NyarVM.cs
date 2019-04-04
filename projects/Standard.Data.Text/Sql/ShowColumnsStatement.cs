namespace Std.Data.Text.Sql;

/// <summary>
///     SHOW COLUMNS FROM table_name 语句
///     。
/// </summary>
public sealed class ShowColumnsStatement : SqlNode
{
    public ShowColumnsStatement(string tableName)
    {
        table_name = tableName;
    }

    public string table_name { get; }

    public override string ToString()
    {
        return $"SHOW COLUMNS FROM {table_name}";
    }
}
namespace Std.Data.Text.Sql;

/// <summary>
///     DESCRIBE table_name 语句
///     。
/// </summary>
public sealed class DescribeTableStatement : SqlNode
{
    public DescribeTableStatement(string tableName)
    {
        table_name = tableName;
    }

    public string table_name { get; }

    public override string ToString()
    {
        return $"DESCRIBE {table_name}";
    }
}
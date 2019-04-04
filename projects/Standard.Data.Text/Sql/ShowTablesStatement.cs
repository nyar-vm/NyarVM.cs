namespace Std.Data.Text.Sql;

/// <summary>
///     SHOW TABLES 语句
///     。
/// </summary>
public sealed class ShowTablesStatement : SqlNode
{
    public override string ToString()
    {
        return "SHOW TABLES";
    }
}
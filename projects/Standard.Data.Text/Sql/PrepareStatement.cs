namespace Std.Data.Text.Sql;

/// <summary>
///     PREPARE 语句 — 创建预处理语句
///     PREPARE stmt_name FROM 'query_string'
///     。
/// </summary>
public sealed class PrepareStatement : SqlNode
{
    public PrepareStatement(string name, string query)
    {
        this.name = name;
        this.query = query;
    }

    public string name { get; }
    public string query { get; }

    public override string ToString()
    {
        return $"PREPARE {name} FROM '{query}'";
    }
}
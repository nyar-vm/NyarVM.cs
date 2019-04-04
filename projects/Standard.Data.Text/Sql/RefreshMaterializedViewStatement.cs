namespace Std.Data.Text.Sql;

/// <summary>
///     REFRESH MATERIALIZED VIEW 语句
///     。
/// </summary>
public sealed class RefreshMaterializedViewStatement : SqlNode
{
    public RefreshMaterializedViewStatement(string name)
    {
        this.name = name;
    }

    public string name { get; }

    public override string ToString()
    {
        return $"REFRESH MATERIALIZED VIEW {name}";
    }
}
namespace Std.Data.Text.Sql;

/// <summary>
///     DROP MATERIALIZED VIEW 语句
///     。
/// </summary>
public sealed class DropMaterializedViewStatement : SqlNode
{
    public DropMaterializedViewStatement(string name, bool ifExists = false)
    {
        this.name = name;
        if_exists = ifExists;
    }

    public string name { get; }
    public bool if_exists { get; }

    public override string ToString()
    {
        return $"DROP MATERIALIZED VIEW {(if_exists ? "IF EXISTS " : "")}{name}";
    }
}
namespace Std.Data.Text.Sql;

/// <summary>
///     CREATE MATERIALIZED VIEW 语句
///     。
/// </summary>
public sealed class CreateMaterializedViewStatement : SqlNode
{
    public CreateMaterializedViewStatement(string name, SelectStatement selectStatement, string? refreshMode,
        bool ifNotExists = false)
    {
        this.name = name;
        select_statement = selectStatement;
        refresh_mode = refreshMode;
        if_not_exists = ifNotExists;
    }

    public string name { get; }
    public SelectStatement select_statement { get; }
    public string? refresh_mode { get; }
    public bool if_not_exists { get; }

    public override string ToString()
    {
        var ifne = if_not_exists ? "IF NOT EXISTS " : "";
        return $"CREATE MATERIALIZED VIEW {ifne}{name} AS {select_statement}";
    }
}
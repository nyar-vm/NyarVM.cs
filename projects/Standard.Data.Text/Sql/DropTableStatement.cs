namespace Std.Data.Text.Sql;

public sealed class DropTableStatement : SqlNode
{
    public DropTableStatement(string table, bool ifExists = false)
    {
        this.table = table;
        if_exists = ifExists;
    }

    public string table { get; }
    public bool if_exists { get; }

    public override string ToString()
    {
        var exists = if_exists ? "IF EXISTS " : "";
        return $"DROP TABLE {exists}{table}";
    }
}
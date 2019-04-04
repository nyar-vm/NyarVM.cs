namespace Std.Data.Text.Sql;

public sealed class CreateTableStatement : SqlNode
{
    public CreateTableStatement(string table, IReadOnlyList<ColumnDef> columns, bool ifNotExists = false)
    {
        this.table = table;
        this.columns = columns;
        if_not_exists = ifNotExists;
    }

    public string table { get; }
    public IReadOnlyList<ColumnDef> columns { get; }
    public bool if_not_exists { get; }

    public override string ToString()
    {
        var exists = if_not_exists ? "IF NOT EXISTS " : "";
        return $"CREATE TABLE {exists}{table} ({string.Join(", ", columns)})";
    }
}
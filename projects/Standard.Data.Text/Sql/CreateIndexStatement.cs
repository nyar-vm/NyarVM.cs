namespace Std.Data.Text.Sql;

/// <summary>
///     CREATE INDEX 语句
///     。
/// </summary>
public sealed class CreateIndexStatement : SqlNode
{
    public CreateIndexStatement(
        bool isUnique,
        bool ifNotExists,
        string? name,
        string table,
        IReadOnlyList<(string Column, bool Descending)> columns)
    {
        is_unique = isUnique;
        if_not_exists = ifNotExists;
        this.name = name;
        this.table = table;
        this.columns = columns;
    }

    public bool is_unique { get; }
    public bool if_not_exists { get; }
    public string? name { get; }
    public string table { get; }
    public IReadOnlyList<(string Column, bool Descending)> columns { get; }

    public override string ToString()
    {
        var unique = is_unique ? "UNIQUE " : "";
        var ifne = if_not_exists ? "IF NOT EXISTS " : "";
        return
            $"CREATE {unique}INDEX {ifne}{name} ON {table} ({string.Join(", ", columns.Select(c => c.Descending ? $"{c.Column} DESC" : c.Column))})";
    }
}
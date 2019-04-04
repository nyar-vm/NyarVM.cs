namespace Std.Data.Text.Sql;

public sealed class InsertStatement : SqlNode
{
    public InsertStatement(
        string table,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<SqlExpression>> valuesRows,
        bool isDefaultValues = false,
        InsertKind kind = InsertKind.insert,
        SelectStatement? selectSource = null,
        IReadOnlyList<SqlColumn>? returning = null,
        OnConflictClause? onConflict = null)
    {
        this.table = table;
        this.columns = columns;
        values_rows = valuesRows;
        is_default_values = isDefaultValues;
        this.kind = kind;
        select_source = selectSource;
        this.returning = returning;
        on_conflict = onConflict;
    }

    public InsertKind kind { get; }
    public string table { get; }
    public IReadOnlyList<string> columns { get; }
    public IReadOnlyList<IReadOnlyList<SqlExpression>> values_rows { get; }
    public bool is_default_values { get; }
    public SelectStatement? select_source { get; }
    public IReadOnlyList<SqlColumn>? returning { get; }
    public OnConflictClause? on_conflict { get; }

    public override string ToString()
    {
        var prefix = kind switch
        {
            InsertKind.replace => "REPLACE",
            InsertKind.insert_or_replace => "INSERT OR REPLACE",
            InsertKind.insert_or_ignore => "INSERT OR IGNORE",
            InsertKind.upsert => "UPSERT",
            _ => "INSERT"
        };

        if (is_default_values) return $"{prefix} INTO {table} DEFAULT VALUES";

        var cols = columns.Count > 0 ? $"({string.Join(", ", columns)})" : "";
        var source = select_source is not null
            ? $" {select_source}"
            : $" VALUES {string.Join(", ", values_rows.Select(r => $"({string.Join(", ", r)})"))}";

        var onConflict = on_conflict is not null ? $" {on_conflict}" : "";

        return $"{prefix} INTO {table}{cols}{source}{onConflict}";
    }
}
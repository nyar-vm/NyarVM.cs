namespace Std.Data.Text.Sql;

public sealed class ColumnDef : SqlNode
{
    public ColumnDef(
        string name,
        string type,
        bool isPrimaryKey = false,
        bool isNotNull = false,
        SqlExpression? defaultValue = null,
        bool isAutoincrement = false,
        bool isUnique = false,
        SqlExpression? checkExpression = null,
        string? collate = null)
    {
        this.name = name;
        this.type = type;
        is_primary_key = isPrimaryKey;
        is_not_null = isNotNull;
        default_value = defaultValue;
        is_autoincrement = isAutoincrement;
        is_unique = isUnique;
        check_expression = checkExpression;
        this.collate = collate;
    }

    public string name { get; }
    public string type { get; }
    public bool is_primary_key { get; }
    public bool is_not_null { get; }
    public bool is_autoincrement { get; }
    public bool is_unique { get; }
    public SqlExpression? default_value { get; }
    public SqlExpression? check_expression { get; }
    public string? collate { get; }

    public override string ToString()
    {
        var parts = new List<string> { name, type };
        if (is_primary_key) parts.Add("PRIMARY KEY");

        if (is_autoincrement) parts.Add("AUTOINCREMENT");

        if (is_not_null) parts.Add("NOT NULL");

        if (is_unique) parts.Add("UNIQUE");

        if (default_value is not null) parts.Add($"DEFAULT {default_value}");

        if (check_expression is not null) parts.Add($"CHECK ({check_expression})");

        if (collate is not null) parts.Add($"COLLATE {collate}");

        return string.Join(" ", parts);
    }
}
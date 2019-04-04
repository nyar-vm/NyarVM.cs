using Hermes.Plugin.Sql.Execution;

namespace Hermes.Plugin.Sql.Migration;

/// <summary>
///     数据库 Schema 内省器 — 读取现有数据库的表结构（基于自研 SqlQueryResult）
/// </summary>
public sealed class SchemaIntrospector
{
    private readonly SqlDialect _dialect;
    private readonly ISqlExecutor _executor;

    public SchemaIntrospector(ISqlExecutor executor)
    {
        _executor = executor;
        _dialect = executor.Dialect;
    }

    /// <summary>
    ///     获取数据库中所有表名
    /// </summary>
    public async Task<List<string>> GetTableNamesAsync()
    {
        var sql = _dialect.Name switch
        {
            "mysql" =>
                "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_TYPE = 'BASE TABLE'",
            "postgresql" => "SELECT tablename FROM pg_tables WHERE schemaname = 'public'",
            "sqlite" => "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'",
            _ => throw new NotSupportedException($"不支持的方言: {_dialect.Name}")
        };

        var result = await _executor.ExecuteQueryAsync(sql);
        if (!result.Success) throw new InvalidOperationException($"获取表名失败：{result.Error}");

        return
        [
            .. result.Rows
                .Select(row => row.Values.FirstOrDefault()?.ToString() ?? "")
                .Where(name => !string.IsNullOrEmpty(name))
        ];
    }

    /// <summary>
    ///     获取表的列信息
    /// </summary>
    public async Task<List<ColumnInfo>> GetColumnsAsync(string tableName)
    {
        var sql = _dialect.Name switch
        {
            "mysql" => $"SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_KEY, COLUMN_DEFAULT, EXTRA " +
                       $"FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' " +
                       $"ORDER BY ORDINAL_POSITION",
            "postgresql" =>
                $"SELECT column_name, data_type, is_nullable, '' as column_key, column_default, '' as extra " +
                $"FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{tableName}' " +
                $"ORDER BY ordinal_position",
            "sqlite" => $"PRAGMA table_info(\"{tableName}\")",
            _ => throw new NotSupportedException($"不支持的方言: {_dialect.Name}")
        };

        var result = await _executor.ExecuteQueryAsync(sql);
        if (!result.Success) throw new InvalidOperationException($"获取列信息失败：{result.Error}");

        var columns = new List<ColumnInfo>();

        foreach (var row in result.Rows)
            if (_dialect.Name == "sqlite")
            {
                columns.Add(new ColumnInfo
                {
                    Name = RowStr(row, 1),
                    DataType = RowStr(row, 2),
                    IsNullable = RowStr(row, 3) == "0",
                    IsPrimaryKey = RowInt(row, 5) == 1,
                    DefaultValue = RowStrNull(row, 4),
                    IsAutoIncrement = RowStr(row, 4)?.Contains("AUTOINCREMENT") ?? false
                });
            }
            else
            {
                var columnKey = RowStr(row, 3);
                var extra = RowStr(row, 5);
                columns.Add(new ColumnInfo
                {
                    Name = RowStr(row, 0),
                    DataType = RowStr(row, 1),
                    IsNullable = RowStr(row, 2) == "YES",
                    IsPrimaryKey = columnKey == "PRI",
                    DefaultValue = RowStrNull(row, 4),
                    IsAutoIncrement = extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase)
                });
            }

        return columns;
    }

    /// <summary>
    ///     获取表的外键信息
    /// </summary>
    public async Task<List<ForeignKeyInfo>> GetForeignKeysAsync(string tableName)
    {
        var sql = _dialect.Name switch
        {
            "mysql" => $"SELECT COLUMN_NAME, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME " +
                       $"FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE " +
                       $"WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = '{tableName}' AND REFERENCED_TABLE_NAME IS NOT NULL",
            "postgresql" => $"SELECT kcu.column_name, ccu.table_name, ccu.column_name " +
                            $"FROM information_schema.key_column_usage kcu " +
                            $"JOIN information_schema.table_constraints tc ON kcu.constraint_name = tc.constraint_name " +
                            $"JOIN information_schema.constraint_column_usage ccu ON ccu.constraint_name = tc.constraint_name " +
                            $"WHERE tc.constraint_type = 'FOREIGN KEY' AND kcu.table_name = '{tableName}'",
            "sqlite" => $"PRAGMA foreign_key_list(\"{tableName}\")",
            _ => throw new NotSupportedException($"不支持的方言: {_dialect.Name}")
        };

        var result = await _executor.ExecuteQueryAsync(sql);
        if (!result.Success) throw new InvalidOperationException($"获取外键信息失败：{result.Error}");

        return
        [
            .. result.Rows.Select(row => new ForeignKeyInfo
            {
                ColumnName = RowStr(row, 0),
                ReferencedTableName = RowStr(row, 1),
                ReferencedColumnName = RowStr(row, 2)
            })
        ];
    }

    /// <summary>
    ///     获取完整的数据库 Schema 快照
    /// </summary>
    public async Task<DatabaseSchema> GetDatabaseSchemaAsync()
    {
        var tables = await GetTableNamesAsync();
        var schema = new DatabaseSchema();

        foreach (var table in tables)
        {
            var columns = await GetColumnsAsync(table);
            var foreignKeys = await GetForeignKeysAsync(table);
            schema.Tables[table] = new TableInfo
            {
                Name = table,
                Columns = columns,
                ForeignKeys = foreignKeys
            };
        }

        return schema;
    }

    /// <summary>
    ///     检查表是否存在
    /// </summary>
    public async Task<bool> TableExistsAsync(string tableName)
    {
        var tables = await GetTableNamesAsync();
        return tables.Contains(tableName);
    }

    private static string RowStr(IReadOnlyDictionary<string, object?> row, int colIndex)
    {
        var key = row.Keys.ElementAtOrDefault(colIndex);
        if (key is null) return "";

        return row[key]?.ToString() ?? "";
    }

    private static string? RowStrNull(IReadOnlyDictionary<string, object?> row, int colIndex)
    {
        var key = row.Keys.ElementAtOrDefault(colIndex);
        if (key is null) return null;

        return row[key]?.ToString();
    }

    private static int RowInt(IReadOnlyDictionary<string, object?> row, int colIndex)
    {
        var key = row.Keys.ElementAtOrDefault(colIndex);
        if (key is null) return 0;

        return row[key] is long l ? (int)l : row[key] is int i ? i : 0;
    }
}

/// <summary>
///     列信息
/// </summary>
public sealed class ColumnInfo
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsAutoIncrement { get; set; }
}

/// <summary>
///     外键信息
/// </summary>
public sealed class ForeignKeyInfo
{
    public string ColumnName { get; set; } = "";
    public string ReferencedTableName { get; set; } = "";
    public string ReferencedColumnName { get; set; } = "";
}

/// <summary>
///     表信息
/// </summary>
public sealed class TableInfo
{
    public string Name { get; set; } = "";
    public List<ColumnInfo> Columns { get; set; } = [];
    public List<ForeignKeyInfo> ForeignKeys { get; set; } = [];
}

/// <summary>
///     数据库 Schema 快照
/// </summary>
public sealed class DatabaseSchema
{
    public Dictionary<string, TableInfo> Tables { get; set; } = [];
}
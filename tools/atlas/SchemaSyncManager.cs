using System.Text;
using Hermes.Compiler;
using Hermes.Plugin.Sql;
using Hermes.Plugin.Sql.Execution;
using Hermes.Plugin.Sql.Migration;

namespace Atlas.CLI;

/// <summary>
///     Schema 同步管理器 — 将 Hermes Schema 同步到数据库，或从数据库拉取 Schema
/// </summary>
public sealed class SchemaSyncManager
{
    private readonly string? _connection_string;
    private readonly bool _force;
    private readonly string _provider;
    private readonly string _schema_path;
    private readonly bool _use_main;

    public SchemaSyncManager(string provider, string? connectionString, string schemaPath, bool force = false,
        bool useMain = false)
    {
        _provider = provider;
        _connection_string = connectionString;
        _schema_path = schemaPath;
        _force = force;
        _use_main = useMain;
    }

    /// <summary>
    ///     将 Schema 同步到数据库（创建/修改表结构）
    /// </summary>
    public async Task<SyncResult> save(bool dryRun)
    {
        try
        {
            var schemaFiles = resolve_schema_files();
            if (schemaFiles.Length == 0) return SyncResult.Fail($"未找到 Schema 文件: {_schema_path}");

            if (string.IsNullOrEmpty(_connection_string)) return SyncResult.Fail("未指定数据库连接字符串");

            var dialect = resolve_dialect();
            var tablesSynced = 0;
            var columnsSynced = 0;
            var warnings = new List<string>();

            foreach (var file in schemaFiles)
            {
                var compiler = new HermesCompiler();
                var result = compiler.Compile(file);

                if (!result.Success)
                {
                    foreach (var diag in result.Diagnostics.Diagnostics)
                        warnings.Add($"[{diag.Level}] {diag.Code}: {diag.Message}");

                    continue;
                }

                var schema = result.Schema!;
                var plugin = new SqlPlugin();

                Console.WriteLine(
                    $"📊 Schema 分析: {schema.Storages.Sum(s => s.Models.Count)} 个 model, {schema.Enums.Count} 个 enum");

                if (dryRun)
                {
                    var options = new SyncOptions
                    {
                        Dialect = dialect,
                        DryRun = true
                    };

                    var syncResult = await plugin.SyncToDatabaseAsync(schema, _connection_string, options);

                    if (syncResult.SqlStatements.Count > 0)
                    {
                        Console.WriteLine($"[DryRun] 需要执行 {syncResult.SqlStatements.Count} 条 SQL:");
                        foreach (var sql in syncResult.SqlStatements) Console.WriteLine($"  [DryRun] {sql}");
                    }
                    else
                    {
                        Console.WriteLine("[DryRun] 数据库已是最新，无需同步");
                    }

                    foreach (var storage in schema.Storages)
                    {
                        tablesSynced += storage.Models.Count;
                        columnsSynced += storage.Models.Sum(m => m.fields.Count);
                    }

                    continue;
                }

                if (!_force)
                {
                    var previewOptions = new SyncOptions { Dialect = dialect, DryRun = true };
                    var preview = await plugin.SyncToDatabaseAsync(schema, _connection_string, previewOptions);

                    if (preview.SqlStatements.Count > 0 && !confirm_save(preview.SqlStatements))
                    {
                        warnings.Add("用户取消了 Save 操作");
                        return new SyncResult
                        {
                            Success = false, TablesSynced = 0, ColumnsSynced = 0, Warnings = warnings,
                            ErrorMessage = "操作已取消"
                        };
                    }
                }

                var syncOptions = new SyncOptions { Dialect = dialect, DryRun = false };
                var syncResult2 = await plugin.SyncToDatabaseAsync(schema, _connection_string, syncOptions);

                if (syncResult2.Success)
                {
                    Console.WriteLine($"✅ 同步完成: {syncResult2.Message}");
                    foreach (var storage in schema.Storages)
                    {
                        tablesSynced += storage.Models.Count;
                        columnsSynced += storage.Models.Sum(m => m.fields.Count);
                    }
                }
                else
                {
                    warnings.Add(syncResult2.ErrorMessage ?? "同步失败");
                }
            }

            return new SyncResult
                { Success = true, TablesSynced = tablesSynced, ColumnsSynced = columnsSynced, Warnings = warnings };
        }
        catch (Exception ex)
        {
            return SyncResult.Fail(ex.Message);
        }
    }

    /// <summary>
    ///     从数据库拉取 Schema，生成 .he/.hermes 文件
    /// </summary>
    public async Task<PullResult> load()
    {
        if (string.IsNullOrEmpty(_connection_string))
            return new PullResult { success = false, error_message = "数据库连接字符串未配置" };

        var dialect = resolve_dialect();

        try
        {
            using var executor = SqlExecutorFactory.Create(_connection_string);
            var isConnected = await executor.TestConnectionAsync();
            if (!isConnected) return new PullResult { success = false, error_message = "数据库连接失败" };

            var introspector = new SchemaIntrospector(executor);
            var tableNames = await introspector.GetTableNamesAsync();

            if (tableNames.Count == 0) return new PullResult { success = false, error_message = "数据库中未发现任何用户表" };

            var sb = new StringBuilder();
            sb.AppendLine("namespace! load {");

            foreach (var tableName in tableNames)
            {
                var columns = await introspector.GetColumnsAsync(tableName);
                var className = to_pascal_case(tableName);

                sb.AppendLine($"    class {className} {{");

                foreach (var col in columns)
                {
                    var hermesType = map_db_type_to_hermes_type(dialect, col.DataType);
                    var prefix = col.IsPrimaryKey ? "@@" : "";
                    var nullable = col.IsNullable ? "?" : "";
                    sb.AppendLine($"        {prefix} {hermesType}{nullable} {col.Name},");
                }

                sb.AppendLine("    }");
                sb.AppendLine();
            }

            sb.AppendLine("}");

            var outputPath = _schema_path ?? "./pulled_schema.hermes";
            await File.WriteAllTextAsync(outputPath, sb.ToString());

            Console.WriteLine($"✅ Load 完成: {tableNames.Count} 张表已拉取到 {outputPath}");
            return new PullResult
            {
                success = true,
                tables_pulled = tableNames.Count
            };
        }
        catch (Exception ex)
        {
            return new PullResult { success = false, error_message = $"Load 失败: {ex.Message}" };
        }
    }

    private string resolve_dialect()
    {
        return _provider.ToLower() switch
        {
            "postgresql" or "pgsql" => "postgresql",
            "mysql" => "mysql",
            _ => "sqlite"
        };
    }

    private static bool confirm_save(List<string> sqlStatements)
    {
        Console.WriteLine($"\n📋 即将执行 {sqlStatements.Count} 条 SQL 语句：");
        foreach (var sql in sqlStatements)
        {
            var preview = sql.Length > 200 ? sql[..200] + "..." : sql;
            Console.WriteLine($"   {preview.Replace('\n', ' ').Trim()}");
        }

        Console.Write("\n确认执行？(y/N) ");
        var input = Console.ReadLine()?.Trim().ToLower();
        return input is "y" or "yes";
    }

    private string[] resolve_schema_files()
    {
        if (File.Exists(_schema_path)) return [_schema_path];

        if (Directory.Exists(_schema_path))
        {
            var files = Directory.GetFiles(_schema_path, "*.hermes", SearchOption.AllDirectories);
            if (files.Length > 0) return files;

            files = Directory.GetFiles(_schema_path, "*.he", SearchOption.AllDirectories);
            if (files.Length > 0) return files;
        }

        var hermesFile = Path.ChangeExtension(_schema_path, ".hermes");
        if (File.Exists(hermesFile)) return [hermesFile];

        var heFile = Path.ChangeExtension(_schema_path, ".he");
        if (File.Exists(heFile)) return [heFile];

        return [];
    }

    private static string to_pascal_case(string snakeCase)
    {
        var parts = snakeCase.Split('_');
        var result = new StringBuilder();
        foreach (var part in parts)
        {
            if (part.Length == 0) continue;

            result.Append(char.ToUpperInvariant(part[0]));
            result.Append(part[1..].ToLowerInvariant());
        }

        return result.ToString();
    }

    private static string map_db_type_to_hermes_type(string dialect, string dbType)
    {
        var upper = dbType.ToUpperInvariant().Split('(', ')')[0].Trim();

        return upper switch
        {
            "INTEGER" or "INT" => "i32",
            "BIGINT" => "i64",
            "SMALLINT" => "i16",
            "TINYINT" => "i8",
            "REAL" or "FLOAT" => "f32",
            "DOUBLE" or "DOUBLE PRECISION" => "f64",
            "BOOLEAN" or "BOOL" => "bool",
            "TEXT" or "VARCHAR" or "CHAR" or "CHARACTER" or "LONGTEXT" or "MEDIUMTEXT" or "TINYTEXT" => "utf8",
            "UUID" => "uuid",
            "BLOB" or "BYTEA" or "VARBINARY" => "list<u8>",
            "DATETIME" or "TIMESTAMP" or "TIMESTAMPTZ" or "DATE" => "datetime",
            "DECIMAL" or "NUMERIC" => "f64",
            "JSON" or "JSONB" => "utf8",
            _ => "utf8"
        };
    }

    public static (string? main, string? test) resolve_database_names_static(SchemaIR schema)
    {
        string? main = null;
        string? test = null;

        foreach (var storage in schema.Storages)
        {
            var dbAttr = storage.Attributes.FirstOrDefault(a => a.Name is "database" or "Database");
            if (dbAttr is null) continue;

            foreach (var arg in dbAttr.Arguments)
            {
                if (arg.Key is "main" or "Main") main = arg.Value;

                if (arg.Key is "test" or "Test") test = arg.Value;
            }
        }

        return (main, test);
    }
}

/// <summary>
///     拉取结果
/// </summary>
public sealed class PullResult
{
    public bool success { get; init; }
    public int tables_pulled { get; init; }
    public string? error_message { get; init; }
}
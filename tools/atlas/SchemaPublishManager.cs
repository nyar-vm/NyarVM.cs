namespace Atlas.CLI;

/// <summary>
///     Schema 发布管理器 — 将 test 数据库的 Schema 变更发布到 main 数据库（全自研协议库）
/// </summary>
public sealed class SchemaPublishManager
{
    private readonly bool _dry_run;
    private readonly bool _force;
    private readonly string? _main_connection_string;
    private readonly string _provider;
    private readonly string _schema_path;
    private readonly string? _test_connection_string;

    /// <summary>
    ///     创建 Schema 发布管理器
    /// </summary>
    /// <param name="provider">数据库提供程序</param>
    /// <param name="testConnectionString">测试数据库连接字符串</param>
    /// <param name="mainConnectionString">生产数据库连接字符串</param>
    /// <param name="schemaPath">Schema 文件路径</param>
    /// <param name="force">是否跳过确认提示</param>
    /// <param name="dryRun">是否只预览不执行</param>
    public SchemaPublishManager(
        string provider,
        string? testConnectionString,
        string? mainConnectionString,
        string schemaPath,
        bool force = false,
        bool dryRun = false)
    {
        _provider = provider;
        _test_connection_string = testConnectionString;
        _main_connection_string = mainConnectionString;
        _schema_path = schemaPath;
        _force = force;
        _dry_run = dryRun;
    }

    /// <summary>
    ///     执行发布流程
    /// </summary>
    public async Task<PublishResult> publish()
    {
        try
        {
            if (string.IsNullOrEmpty(_main_connection_string)) return PublishResult.fail("未配置 main 数据库连接字符串，无法发布到生产环境");

            if (string.IsNullOrEmpty(_test_connection_string))
                return PublishResult.fail("未配置 test 数据库连接字符串，无法比较 schema");

            var schemaFiles = resolve_schema_files();
            if (schemaFiles.Length == 0) return PublishResult.fail($"未找到 Schema 文件: {_schema_path}");

            var dialect = _provider.ToLower() switch
            {
                "postgresql" or "pgsql" => "postgresql",
                "mysql" => "mysql",
                _ => "sqlite"
            };

            var sqlDialect = SqlDialect.FromName(dialect);
            var allDiffs = new SchemaDiffResult();
            var allSqlStatements = new List<string>();

            foreach (var file in schemaFiles)
            {
                var compiler = new HermesCompiler();
                var result = compiler.Compile(file);

                if (!result.Success)
                {
                    var errors = result.Diagnostics.Diagnostics
                        .Select(d => $"[{d.Level}] {d.Code}: {d.Message}");
                    return PublishResult.fail($"Schema 编译失败:\n  {string.Join("\n  ", errors)}");
                }

                var schema = result.Schema!;
                var dbNames = SchemaSyncManager.resolve_database_names_static(schema);
                var testDb = dbNames.test ?? dbNames.main ?? "default";
                var mainDb = dbNames.main ?? dbNames.test ?? "default";

                var testSchema = await read_database_schema(dialect, _test_connection_string);
                var mainSchema = await read_database_schema(dialect, _main_connection_string);

                var diff = compute_schema_diff(mainSchema, testSchema);

                if (diff.TableDiffs.Count == 0)
                {
                    Console.WriteLine($"✅ {mainDb} 与 {testDb} 的 schema 完全一致，无需发布");
                    continue;
                }

                var ddlGenerator = new DdlGenerator(sqlDialect);
                var sqlStatements = ddlGenerator.GenerateMigrationSql(diff);
                allDiffs.TableDiffs.AddRange(diff.TableDiffs);
                allSqlStatements.AddRange(sqlStatements);
            }

            if (allDiffs.TableDiffs.Count == 0)
                return new PublishResult
                {
                    success = true,
                    changes_applied = 0,
                    message = "test 和 main 的 schema 完全一致，无需发布"
                };

            var totalChanges = count_total_changes(allDiffs);
            print_diff_summary(allDiffs);

            Console.WriteLine($"\n📋 即将在 main 数据库执行 {allSqlStatements.Count} 条 SQL 语句：");
            foreach (var sql in allSqlStatements)
            {
                var preview = sql.Length > 150 ? sql[..150] + "..." : sql;
                Console.WriteLine($"   {preview.Replace('\n', ' ').Trim()}");
            }

            if (_dry_run)
            {
                Console.WriteLine("\n🏃 DryRun 模式，未实际执行");
                return new PublishResult
                {
                    success = true,
                    changes_applied = totalChanges,
                    message = "DryRun 模式，未实际执行"
                };
            }

            if (!_force)
            {
                Console.Write("\n⚠️  确认将以上变更发布到 main 数据库？(y/N) ");
                var input = Console.ReadLine()?.Trim().ToLower();
                if (input is not ("y" or "yes")) return PublishResult.fail("用户取消了发布操作");
            }

            var executed = await execute_migration(allSqlStatements, dialect);

            return executed
                ? new PublishResult
                {
                    success = true,
                    changes_applied = totalChanges,
                    message = $"已将 {totalChanges} 项变更发布到 main 数据库"
                }
                : PublishResult.fail("SQL 执行失败，请检查日志");
        }
        catch (Exception ex)
        {
            return PublishResult.fail($"发布失败: {ex.Message}");
        }
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

    private static async Task<List<TableInfo>> read_database_schema(string dialect, string connectionString)
    {
        var tables = new List<TableInfo>();

        try
        {
            var sqlDialect = SqlDialect.FromName(dialect);
            using var executor = SqlExecutorFactory.Create(sqlDialect, connectionString);
            var isConnected = await executor.TestConnectionAsync();
            if (!isConnected)
            {
                Console.WriteLine("⚠️ 数据库连接失败");
                return tables;
            }

            var introspector = new SchemaIntrospector(executor);
            var tableNames = await introspector.GetTableNamesAsync();

            foreach (var tableName in tableNames)
            {
                var columns = await introspector.GetColumnsAsync(tableName);
                tables.Add(new TableInfo
                {
                    Name = tableName,
                    Columns = columns
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ 读取数据库 schema 失败: {ex.Message}");
        }

        return tables;
    }

    /// <summary>
    ///     计算两个数据库 schema 的差异，返回 SchemaDiffResult（与 DdlGenerator 类型一致）
    /// </summary>
    private static SchemaDiffResult compute_schema_diff(
        List<TableInfo> mainSchema,
        List<TableInfo> testSchema)
    {
        var result = new SchemaDiffResult();
        var mainTables = mainSchema.ToDictionary(t => t.Name.ToLower());
        var testTables = testSchema.ToDictionary(t => t.Name.ToLower());

        foreach (var (name, testTable) in testTables)
        {
            if (!mainTables.TryGetValue(name, out var mainTable))
            {
                result.TableDiffs.Add(new TableDiff
                {
                    Type = SchemaDiffType.TableAdded,
                    TableName = name
                });
                continue;
            }

            var mainCols = mainTable.Columns.ToDictionary(c => c.Name.ToLower());
            var testCols = testTable.Columns.ToDictionary(c => c.Name.ToLower());
            var tableDiff = new TableDiff
            {
                Type = SchemaDiffType.TableModified,
                TableName = name
            };
            var hasColumnDiff = false;

            foreach (var (colName, testCol) in testCols)
                if (!mainCols.TryGetValue(colName, out var mainCol))
                {
                    tableDiff.ColumnDiffs.Add(new ColumnDiff
                    {
                        Type = SchemaDiffType.ColumnAdded,
                        ColumnName = colName,
                        NewType = testCol.DataType
                    });
                    hasColumnDiff = true;
                }
                else if (!column_equals(mainCol, testCol))
                {
                    tableDiff.ColumnDiffs.Add(new ColumnDiff
                    {
                        Type = SchemaDiffType.ColumnModified,
                        ColumnName = colName,
                        OldType = mainCol.DataType,
                        NewType = testCol.DataType
                    });
                    hasColumnDiff = true;
                }

            foreach (var (colName, _) in mainCols)
                if (!testCols.ContainsKey(colName))
                {
                    tableDiff.ColumnDiffs.Add(new ColumnDiff
                    {
                        Type = SchemaDiffType.ColumnRemoved,
                        ColumnName = colName
                    });
                    hasColumnDiff = true;
                }

            if (hasColumnDiff) result.TableDiffs.Add(tableDiff);
        }

        foreach (var (name, _) in mainTables)
            if (!testTables.ContainsKey(name))
                result.TableDiffs.Add(new TableDiff
                {
                    Type = SchemaDiffType.TableRemoved,
                    TableName = name
                });

        return result;
    }

    private static bool column_equals(ColumnInfo a, ColumnInfo b)
    {
        return string.Equals(normalize_type(a.DataType), normalize_type(b.DataType), StringComparison.OrdinalIgnoreCase)
               && a.IsNullable == b.IsNullable
               && a.IsPrimaryKey == b.IsPrimaryKey;
    }

    private static string normalize_type(string type)
    {
        return type.ToUpperInvariant().Split('(', ')')[0].Trim();
    }

    /// <summary>
    ///     统计差异总数（表级 + 列级）
    /// </summary>
    private static int count_total_changes(SchemaDiffResult diff)
    {
        return diff.TableDiffs.Sum(t =>
        {
            if (t.type is SchemaDiffType.TableAdded or SchemaDiffType.TableRemoved) return 1;

            return t.ColumnDiffs.Count;
        });
    }

    /// <summary>
    ///     打印差异概览
    /// </summary>
    private static void print_diff_summary(SchemaDiffResult diff)
    {
        Console.WriteLine("\n📊 Schema 差异分析 (test → main):");
        Console.WriteLine($"   {'=',-60}");

        foreach (var tableDiff in diff.TableDiffs)
            switch (tableDiff.type)
            {
                case SchemaDiffType.TableAdded:
                    Console.WriteLine($"   ➕ 新建表 {tableDiff.TableName}");
                    break;
                case SchemaDiffType.TableRemoved:
                    Console.WriteLine($"   ➖ 删除表 {tableDiff.TableName}");
                    break;
                case SchemaDiffType.TableModified:
                    foreach (var colDiff in tableDiff.ColumnDiffs)
                    {
                        var (icon, desc) = colDiff.type switch
                        {
                            SchemaDiffType.ColumnAdded =>
                                ("➕", $"表 {tableDiff.TableName} 新增列 {colDiff.ColumnName} ({colDiff.NewType})"),
                            SchemaDiffType.ColumnRemoved =>
                                ("➖", $"表 {tableDiff.TableName} 删除列 {colDiff.ColumnName}"),
                            SchemaDiffType.ColumnModified =>
                                ("🔄",
                                    $"表 {tableDiff.TableName} 修改列 {colDiff.ColumnName}: {colDiff.OldType} → {colDiff.NewType}"),
                            _ => ("❓", $"表 {tableDiff.TableName} 未知列变更 {colDiff.ColumnName}")
                        };

                        Console.WriteLine($"   {icon} {desc}");
                    }

                    break;
            }
    }

    private async Task<bool> execute_migration(List<string> sqlStatements, string dialect)
    {
        if (string.IsNullOrEmpty(_main_connection_string)) return false;

        try
        {
            var sqlDialect = SqlDialect.FromName(dialect);
            using var executor = SqlExecutorFactory.Create(sqlDialect, _main_connection_string);

            var isConnected = await executor.TestConnectionAsync();
            if (!isConnected)
            {
                Console.WriteLine("❌ 无法连接到 main 数据库");
                return false;
            }

            Console.WriteLine($"📌 已连接到 {dialect} main 数据库: {executor.DatabaseName}");

            foreach (var sql in sqlStatements)
            {
                var subStatements = sql.Split(';',
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var sub in subStatements)
                {
                    if (string.IsNullOrWhiteSpace(sub)) continue;

                    try
                    {
                        await executor.ExecuteNonQueryAsync(sub);
                        Console.WriteLine($"   ✅ {sub.truncate(80)}");
                    }
                    catch (Exception ex)
                    {
                        if (_force)
                            Console.WriteLine($"   ⚠️ 跳过错误: {ex.Message}");
                        else
                            throw;
                    }
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 执行迁移失败: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
///     发布结果
/// </summary>
public sealed class PublishResult
{
    /// <summary>
    ///     是否成功
    /// </summary>
    public bool success { get; init; }

    /// <summary>
    ///     已应用的变更数
    /// </summary>
    public int changes_applied { get; init; }

    /// <summary>
    ///     成功消息
    /// </summary>
    public string? message { get; init; }

    /// <summary>
    ///     错误消息
    /// </summary>
    public string? error_message { get; init; }

    /// <summary>
    ///     创建失败结果
    /// </summary>
    public static PublishResult fail(string message)
    {
        return new PublishResult { success = false, error_message = message };
    }
}

internal static class StringExtensions
{
    /// <summary>
    ///     截断字符串到指定长度
    /// </summary>
    public static string truncate(this string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
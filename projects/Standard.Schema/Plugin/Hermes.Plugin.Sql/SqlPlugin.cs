using Hermes.Generator;
using Hermes.Plugin.Sql.Ddl;
using Hermes.Plugin.Sql.Dml;
using Hermes.Plugin.Sql.Execution;
using Hermes.Plugin.Sql.Migration;

namespace Hermes.Plugin.Sql;

/// <summary>
///     Hermes SQL 插件 — 完整的 SQL 能力（DDL/DML 生成 + 数据库执行 + Schema 迁移）
/// </summary>
public sealed class SqlPlugin : IGenerator
{
    /// <summary>
    ///     插件名称
    /// </summary>
    public string Name => "sql";

    /// <summary>
    ///     支持的目标类型
    /// </summary>
    public string[] SupportedTargets => ["mysql", "postgresql", "sqlite"];

    /// <summary>
    ///     生成 DDL/DML 文件（IGenerator 接口实现）
    /// </summary>
    public GeneratorResult Generate(GeneratorContext context)
    {
        var dialect = GetDialect(context.Options);
        var includeDml = GetOption(context.Options, "include-dml", false);

        var ddlGenerator = new DdlGenerator(dialect);
        var files = ddlGenerator.GenerateDdlFiles(context.Schema, context.OutputPath, context.SchemaPath);

        if (includeDml)
        {
            var dmlGenerator = new DmlGenerator(dialect);
            foreach (var storage in context.Schema.Storages)
            foreach (var model in storage.Models)
                files.Add(dmlGenerator.GenerateDmlFile(model, context.Schema, context.OutputPath, context.SchemaPath));

            if (context.Schema.Storages.Count == 0)
                foreach (var classDef in context.Schema.Classes)
                {
                    var tableName = SqlTypeMapper.ToSnakeCase(classDef.Name);
                    var dmlGenerator2 = new DmlGenerator(dialect);
                    // DML 生成器目前只支持 Model，对 Class 生成简单的 DML
                }
        }

        return new GeneratorResult { Files = files };
    }

    /// <summary>
    ///     同步 Schema 到数据库（创建/修改表结构）
    /// </summary>
    public async Task<SyncResult> SyncToDatabaseAsync(SchemaIR schema, string connectionString,
        SyncOptions? options = null)
    {
        options ??= new SyncOptions();
        var dialect = SqlDialect.FromName(options.Dialect ?? InferDialect(connectionString));

        using var executor = SqlExecutorFactory.Create(dialect, connectionString);
        var introspector = new SchemaIntrospector(executor);
        var differ = new SchemaDiffer(dialect);

        try
        {
            var isConnected = await executor.TestConnectionAsync();
            if (!isConnected) return SyncResult.Fail("数据库连接失败");

            var dbSchema = await introspector.GetDatabaseSchemaAsync();
            var diffResult = differ.Diff(schema, dbSchema);

            if (!diffResult.TableDiffs.Any()) return new SyncResult { Success = true, Message = "数据库已是最新，无需同步" };

            var syncSql = differ.GenerateSyncSql(schema, dbSchema);

            if (options.DryRun)
                return new SyncResult
                {
                    Success = true,
                    Message = $"[DryRun] 需要执行 {syncSql.Count} 条 SQL",
                    SqlStatements = syncSql,
                    DiffResult = diffResult
                };

            var results = await executor.ExecuteNonQueryBatchAsync(syncSql);
            return new SyncResult
            {
                Success = true,
                Message = $"同步完成，执行了 {syncSql.Count} 条 SQL",
                SqlStatements = syncSql,
                DiffResult = diffResult,
                AffectedRows = results.Sum()
            };
        }
        catch (Exception ex)
        {
            return SyncResult.Fail($"同步失败: {ex.Message}");
        }
    }

    /// <summary>
    ///     读取数据库现有 Schema
    /// </summary>
    public async Task<DatabaseSchema> IntrospectDatabaseAsync(string connectionString, string? dialect = null)
    {
        var sqlDialect = SqlDialect.FromName(dialect ?? InferDialect(connectionString));
        using var executor = SqlExecutorFactory.Create(sqlDialect, connectionString);
        var introspector = new SchemaIntrospector(executor);
        return await introspector.GetDatabaseSchemaAsync();
    }

    /// <summary>
    ///     执行 DDL 到数据库
    /// </summary>
    public async Task<ExecuteResult> ExecuteDdlAsync(SchemaIR schema, string connectionString, string? dialect = null)
    {
        var sqlDialect = SqlDialect.FromName(dialect ?? InferDialect(connectionString));
        using var executor = SqlExecutorFactory.Create(sqlDialect, connectionString);
        var ddlGenerator = new DdlGenerator(sqlDialect);
        var statements = ddlGenerator.GenerateDdlStatements(schema);

        try
        {
            var results = await executor.ExecuteNonQueryBatchAsync(statements);
            return new ExecuteResult
            {
                Success = true,
                StatementsExecuted = results.Length,
                AffectedRows = results.Sum()
            };
        }
        catch (Exception ex)
        {
            return new ExecuteResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    ///     执行原始 SQL
    /// </summary>
    public async Task<ExecuteResult> ExecuteRawSqlAsync(string connectionString, string sql, string? dialect = null)
    {
        var sqlDialect = SqlDialect.FromName(dialect ?? InferDialect(connectionString));
        using var executor = SqlExecutorFactory.Create(sqlDialect, connectionString);

        try
        {
            var affected = await executor.ExecuteNonQueryAsync(sql);
            return new ExecuteResult { Success = true, StatementsExecuted = 1, AffectedRows = affected };
        }
        catch (Exception ex)
        {
            return new ExecuteResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    ///     生成 DDL SQL 语句列表（不执行）
    /// </summary>
    public List<string> GenerateDdlStatements(SchemaIR schema, string dialect = "mysql")
    {
        var sqlDialect = SqlDialect.FromName(dialect);
        var ddlGenerator = new DdlGenerator(sqlDialect);
        return ddlGenerator.GenerateDdlStatements(schema);
    }

    /// <summary>
    ///     生成 DML SQL 语句（指定 Model）
    /// </summary>
    public DmlGenerator GetDmlGenerator(string dialect = "mysql")
    {
        return new DmlGenerator(SqlDialect.FromName(dialect));
    }

    /// <summary>
    ///     创建 SQL 执行器
    /// </summary>
    public ISqlExecutor CreateExecutor(string connectionString, string? dialect = null)
    {
        return SqlExecutorFactory.Create(SqlDialect.FromName(dialect ?? InferDialect(connectionString)),
            connectionString);
    }

    private static SqlDialect GetDialect(IReadOnlyDictionary<string, object> options)
    {
        var target = options.TryGetValue("targets", out var t) ? t.ToString() ?? "mysql" : "mysql";
        return SqlDialect.FromName(target);
    }

    private static T GetOption<T>(IReadOnlyDictionary<string, object> options, string key, T defaultValue)
    {
        if (!options.TryGetValue(key, out var value)) return defaultValue;

        if (value is T typed) return typed;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }

    private static string InferDialect(string connectionString)
    {
        var lower = connectionString.ToLowerInvariant();
        if (lower.Contains("mysql")) return "mysql";

        if (lower.Contains("postgres") || lower.Contains("pgsql")) return "postgresql";

        if (lower.Contains("sqlite") || lower.Contains(".db")) return "sqlite";

        return "mysql";
    }
}

/// <summary>
///     同步选项
/// </summary>
public sealed class SyncOptions
{
    /// <summary>
    ///     数据库方言（null 则自动推断）
    /// </summary>
    public string? Dialect { get; set; }

    /// <summary>
    ///     是否只预览不执行
    /// </summary>
    public bool DryRun { get; set; }

    /// <summary>
    ///     是否包含 DDL 文件生成
    /// </summary>
    public bool IncludeDdl { get; set; } = true;

    /// <summary>
    ///     是否包含 DML 文件生成
    /// </summary>
    public bool IncludeDml { get; set; }
}

/// <summary>
///     同步结果
/// </summary>
public sealed class SyncResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = "";
    public List<string> SqlStatements { get; init; } = [];
    public SchemaDiffResult? DiffResult { get; init; }
    public int AffectedRows { get; init; }
    public string? ErrorMessage { get; init; }
    public int TablesSynced { get; init; }
    public int ColumnsSynced { get; init; }
    public List<string> Warnings { get; init; } = [];

    public static SyncResult Fail(string message)
    {
        return new SyncResult { Success = false, ErrorMessage = message };
    }
}

/// <summary>
///     执行结果
/// </summary>
public sealed class ExecuteResult
{
    public bool Success { get; init; }
    public int StatementsExecuted { get; init; }
    public int AffectedRows { get; init; }
    public string? ErrorMessage { get; init; }

    public static ExecuteResult Fail(string message)
    {
        return new ExecuteResult { Success = false, ErrorMessage = message };
    }
}
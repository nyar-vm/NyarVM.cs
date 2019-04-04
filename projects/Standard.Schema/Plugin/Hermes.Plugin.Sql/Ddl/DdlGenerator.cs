using System.Text;
using Hermes.Generator;

namespace Hermes.Plugin.Sql.Ddl;

/// <summary>
///     DDL 生成器 — 根据 SchemaIR 生成 CREATE TABLE / CREATE INDEX 语句
/// </summary>
public sealed class DdlGenerator
{
    private readonly SqlDialect _dialect;
    private readonly SqlTypeMapper _mapper;

    public DdlGenerator(SqlDialect dialect)
    {
        _dialect = dialect;
        _mapper = new SqlTypeMapper(dialect);
    }

    /// <summary>
    ///     为所有 Storage 中的 Model 生成 DDL 文件
    /// </summary>
    public List<GeneratedFile> GenerateDdlFiles(SchemaIR schema, string outputPath, string schemaPath)
    {
        var files = new List<GeneratedFile>();

        foreach (var storage in schema.Storages)
        foreach (var model in storage.Models)
            files.Add(GenerateTableFile(model, schema, outputPath, schemaPath));

        if (schema.Storages.Count == 0)
            foreach (var classDef in schema.Classes)
                files.Add(GenerateTableFromClassFile(classDef, schema, outputPath, schemaPath));

        var indexContent = GenerateIndexesContent(schema);
        if (indexContent.Length > 0)
            files.Add(new GeneratedFile
            {
                Path = Path.Combine(outputPath, "indexes.sql"),
                Content = indexContent.ToString(),
                Generator = "sql-ddl"
            });

        return files;
    }

    /// <summary>
    ///     为所有 Storage 中的 Model 生成 DDL SQL 语句列表
    /// </summary>
    public List<string> GenerateDdlStatements(SchemaIR schema)
    {
        var statements = new List<string>();

        foreach (var storage in schema.Storages)
        foreach (var model in storage.Models)
            statements.Add(GenerateCreateTableSql(model, schema));

        if (schema.Storages.Count == 0)
            foreach (var classDef in schema.Classes)
                statements.Add(GenerateCreateTableFromClassSql(classDef, schema));

        var indexStatements = GenerateIndexStatements(schema);
        statements.AddRange(indexStatements);

        return statements;
    }

    /// <summary>
    ///     生成单个 Model 的 CREATE TABLE SQL
    /// </summary>
    public string GenerateCreateTableSql(ModelDefinition model, SchemaIR schema)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        return GenerateCreateTableSqlCore(tableName, model.fields, schema);
    }

    /// <summary>
    ///     生成单个 Class 的 CREATE TABLE SQL
    /// </summary>
    public string GenerateCreateTableFromClassSql(ClassDefinition classDef, SchemaIR schema)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(classDef.Name);
        return GenerateCreateTableSqlCore(tableName, classDef.fields, schema);
    }

    /// <summary>
    ///     生成 ALTER TABLE 迁移 SQL
    /// </summary>
    public List<string> GenerateMigrationSql(SchemaDiffResult diff)
    {
        var statements = new List<string>();

        foreach (var tableDiff in diff.TableDiffs)
            switch (tableDiff.type)
            {
                case SchemaDiffType.TableAdded:
                    statements.Add($"-- TODO: CREATE TABLE {_dialect.QuoteIdentifier(tableDiff.TableName)}");
                    break;
                case SchemaDiffType.TableRemoved:
                    statements.Add($"DROP TABLE IF EXISTS {_dialect.QuoteIdentifier(tableDiff.TableName)}");
                    break;
                case SchemaDiffType.TableModified:
                    foreach (var colDiff in tableDiff.ColumnDiffs)
                        switch (colDiff.type)
                        {
                            case SchemaDiffType.ColumnAdded:
                                statements.Add($"ALTER TABLE {_dialect.QuoteIdentifier(tableDiff.TableName)} " +
                                               $"ADD COLUMN {_dialect.QuoteIdentifier(colDiff.ColumnName)} {colDiff.NewType ?? "TEXT"}");
                                break;
                            case SchemaDiffType.ColumnRemoved when _dialect.SupportsDropColumn:
                                statements.Add($"ALTER TABLE {_dialect.QuoteIdentifier(tableDiff.TableName)} " +
                                               $"DROP COLUMN {_dialect.QuoteIdentifier(colDiff.ColumnName)}");
                                break;
                            case SchemaDiffType.ColumnModified when _dialect.SupportsAlterColumn:
                                var alterSql = _dialect.Name switch
                                {
                                    "mysql" => $"ALTER TABLE {_dialect.QuoteIdentifier(tableDiff.TableName)} " +
                                               $"MODIFY COLUMN {_dialect.QuoteIdentifier(colDiff.ColumnName)} {colDiff.NewType ?? "TEXT"}",
                                    "postgresql" => $"ALTER TABLE {_dialect.QuoteIdentifier(tableDiff.TableName)} " +
                                                    $"ALTER COLUMN {_dialect.QuoteIdentifier(colDiff.ColumnName)} TYPE {colDiff.NewType ?? "TEXT"}",
                                    _ => "-- SQLite 不支持 ALTER COLUMN"
                                };
                                statements.Add(alterSql);
                                break;
                        }

                    break;
            }

        return statements;
    }

    private string GenerateCreateTableSqlCore(string tableName, IReadOnlyList<FieldDefinition> fields, SchemaIR schema)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CREATE TABLE IF NOT EXISTS {_dialect.QuoteIdentifier(tableName)} (");

        var columns = new List<string>();
        var foreignKeys = new List<string>();
        var primaryKey = "";
        var primaryKeyInline = false;

        foreach (var field in fields)
        {
            if (field.IsFlatten && SqlTypeMapper.TryGetClassFields(field.FieldType, schema, out var flattenFields))
            {
                var prefix = SqlTypeMapper.ToSnakeCase(field.Name) + "_";
                foreach (var ff in flattenFields!)
                {
                    var ffColName = prefix + SqlTypeMapper.ToSnakeCase(ff.Name);
                    var (ffColType, ffIsNullable, ffFkInfo) =
                        _mapper.ResolveFieldType(ff.FieldType, ff.IsOptional, schema);
                    var ffNullable = ffIsNullable ? "" : " NOT NULL";
                    var ffDefaultVal = ff.DefaultValue != null
                        ? $" DEFAULT {_dialect.FormatDefaultValue(ff.DefaultValue)}"
                        : "";

                    if (ffFkInfo is not null)
                    {
                        ffColType = ffFkInfo.colType;
                        foreignKeys.Add(
                            $"    FOREIGN KEY ({_dialect.QuoteIdentifier(ffColName)}) REFERENCES {_dialect.QuoteIdentifier(ffFkInfo.refTableName)}({_dialect.QuoteIdentifier(ffFkInfo.refKeyName)})");
                    }

                    columns.Add($"    {_dialect.QuoteIdentifier(ffColName)} {ffColType}{ffNullable}{ffDefaultVal}");
                }

                continue;
            }

            var colName = SqlTypeMapper.ToSnakeCase(field.Name);
            var (colType, isNullable, fkInfo) = _mapper.ResolveFieldType(field.FieldType, field.IsOptional, schema);
            var nullable = isNullable ? "" : " NOT NULL";
            var defaultVal = field.DefaultValue != null
                ? $" DEFAULT {_dialect.FormatDefaultValue(field.DefaultValue)}"
                : "";

            if (field.IsPrimaryKey)
            {
                primaryKey = colName;
                if (field.FieldType is PrimitiveType { TypeName: "i32" or "i64" })
                {
                    if (_dialect.Name == "postgresql")
                    {
                        colType = "SERIAL";
                    }
                    else if (_dialect.Name == "mysql")
                    {
                        colType = $"INT {_dialect.AutoIncrementSyntax}";
                    }
                    else if (_dialect.Name == "sqlite")
                    {
                        colType = $"INTEGER PRIMARY KEY {_dialect.AutoIncrementSyntax}";
                        primaryKeyInline = true;
                    }
                }
            }

            if (field.IsUniqueKey) nullable += " UNIQUE";

            if (fkInfo is not null)
            {
                colType = fkInfo.colType;
                foreignKeys.Add(
                    $"    FOREIGN KEY ({_dialect.QuoteIdentifier(colName)}) REFERENCES {_dialect.QuoteIdentifier(fkInfo.refTableName)}({_dialect.QuoteIdentifier(fkInfo.refKeyName)})");
            }

            columns.Add($"    {_dialect.QuoteIdentifier(colName)} {colType}{nullable}{defaultVal}");
        }

        if (!string.IsNullOrEmpty(primaryKey) && !primaryKeyInline)
            columns.Add($"    PRIMARY KEY ({_dialect.QuoteIdentifier(primaryKey)})");

        columns.AddRange(foreignKeys);
        sb.AppendLine(string.Join(",\n", columns));
        sb.AppendLine(");");

        return sb.ToString();
    }

    private GeneratedFile GenerateTableFile(ModelDefinition model, SchemaIR schema, string outputPath,
        string schemaPath)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        var header = GenerateFileHeader(schemaPath);
        var sql = GenerateCreateTableSql(model, schema);

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{tableName}.sql"),
            Content = header + sql,
            Generator = "sql-ddl"
        };
    }

    private GeneratedFile GenerateTableFromClassFile(ClassDefinition classDef, SchemaIR schema, string outputPath,
        string schemaPath)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(classDef.Name);
        var header = GenerateFileHeader(schemaPath);
        var sql = GenerateCreateTableFromClassSql(classDef, schema);

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{tableName}.sql"),
            Content = header + sql,
            Generator = "sql-ddl"
        };
    }

    private StringBuilder GenerateIndexesContent(SchemaIR schema)
    {
        var sb = new StringBuilder();

        foreach (var storage in schema.Storages)
        {
            GenerateIndexesForAttributes(sb, storage.Name, storage.Attributes);

            foreach (var model in storage.Models) GenerateIndexesForAttributes(sb, model.Name, model.Attributes);
        }

        return sb;
    }

    private void GenerateIndexesForAttributes(StringBuilder sb, string targetName,
        IReadOnlyList<Nyar.Dialect.Schema.IR.Common.AttributeDefinition> attributes)
    {
        foreach (var attr in attributes)
        {
            if (attr.Name != "index") continue;

            var columns = attr.Arguments
                .Where(a => a.Key == "columns" || a.Key == "fields")
                .Select(a => a.Value)
                .FirstOrDefault();

            if (columns == null) continue;

            var colNames = columns.Split(',')
                .Select(c => _dialect.QuoteIdentifier(SqlTypeMapper.ToSnakeCase(c.Trim())));
            var indexName =
                $"idx_{SqlTypeMapper.ToSnakeCase(targetName)}_{SqlTypeMapper.ToSnakeCase(columns.Replace(",", "_").Replace(" ", ""))}";
            sb.AppendLine(
                $"CREATE INDEX IF NOT EXISTS {_dialect.QuoteIdentifier(indexName)} ON {_dialect.QuoteIdentifier(SqlTypeMapper.ToSnakeCase(targetName))} ({string.Join(", ", colNames)});");
            sb.AppendLine();
        }
    }

    private List<string> GenerateIndexStatements(SchemaIR schema)
    {
        var statements = new List<string>();
        var content = GenerateIndexesContent(schema);
        if (content.Length > 0)
        {
            var subStatements = content.ToString()
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var sub in subStatements)
                if (!string.IsNullOrWhiteSpace(sub))
                    statements.Add(sub + ";");
        }

        return statements;
    }

    private string GenerateFileHeader(string schemaPath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("-- <auto-generated>");
        sb.AppendLine("-- 本文件由 atlas/hermes 命令自动生成，修改无效");
        sb.AppendLine($"-- 源文件: {schemaPath}");
        sb.AppendLine($"-- 生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("-- </auto-generated>");
        sb.AppendLine();
        return sb.ToString();
    }
}

/// <summary>
///     Schema 差异比较结果
/// </summary>
public sealed class SchemaDiffResult
{
    public List<TableDiff> TableDiffs { get; init; } = [];
}

/// <summary>
///     表级差异
/// </summary>
public sealed class TableDiff
{
    public SchemaDiffType Type { get; init; }
    public string TableName { get; init; } = "";
    public List<ColumnDiff> ColumnDiffs { get; init; } = [];
}

/// <summary>
///     列级差异
/// </summary>
public sealed class ColumnDiff
{
    public SchemaDiffType Type { get; init; }
    public string ColumnName { get; init; } = "";
    public string? OldType { get; init; }
    public string? NewType { get; init; }
}

/// <summary>
///     差异类型
/// </summary>
public enum SchemaDiffType
{
    TableAdded,
    TableRemoved,
    TableModified,
    ColumnAdded,
    ColumnRemoved,
    ColumnModified
}
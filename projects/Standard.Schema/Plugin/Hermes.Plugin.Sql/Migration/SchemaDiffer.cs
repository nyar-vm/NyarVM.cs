using Hermes.Plugin.Sql.Ddl;
using DdlDiff = Hermes.Plugin.Sql.Ddl.SchemaDiffType;

namespace Hermes.Plugin.Sql.Migration;

/// <summary>
///     Schema 差异比较器 — 比较 SchemaIR 与数据库现有结构的差异
/// </summary>
public sealed class SchemaDiffer
{
    private readonly SqlDialect _dialect;
    private readonly SqlTypeMapper _mapper;

    public SchemaDiffer(SqlDialect dialect)
    {
        _dialect = dialect;
        _mapper = new SqlTypeMapper(dialect);
    }

    /// <summary>
    ///     比较 SchemaIR 与数据库现有 Schema 的差异
    /// </summary>
    public SchemaDiffResult Diff(SchemaIR schema, DatabaseSchema dbSchema)
    {
        var result = new SchemaDiffResult();
        var schemaTables = GetSchemaTables(schema);
        var dbTableNames = new HashSet<string>(dbSchema.Tables.Keys);

        foreach (var (tableName, columns) in schemaTables)
        {
            if (!dbTableNames.Contains(tableName))
            {
                result.TableDiffs.Add(new TableDiff
                {
                    Type = DdlDiff.TableAdded,
                    TableName = tableName
                });
                continue;
            }

            var dbTable = dbSchema.Tables[tableName];
            var columnDiffs = DiffColumns(tableName, columns, dbTable.Columns);

            if (columnDiffs.Count > 0)
                result.TableDiffs.Add(new TableDiff
                {
                    Type = DdlDiff.TableModified,
                    TableName = tableName,
                    ColumnDiffs = columnDiffs
                });
        }

        foreach (var dbTableName in dbTableNames)
            if (!schemaTables.ContainsKey(dbTableName))
                result.TableDiffs.Add(new TableDiff
                {
                    Type = DdlDiff.TableRemoved,
                    TableName = dbTableName
                });

        return result;
    }

    /// <summary>
    ///     生成同步到数据库所需的 SQL 语句
    /// </summary>
    public List<string> GenerateSyncSql(SchemaIR schema, DatabaseSchema dbSchema)
    {
        var statements = new List<string>();
        var diffResult = Diff(schema, dbSchema);
        var ddlGenerator = new DdlGenerator(_dialect);

        foreach (var tableDiff in diffResult.TableDiffs)
            switch (tableDiff.type)
            {
                case DdlDiff.TableAdded:
                {
                    var model = FindModel(schema, tableDiff.TableName);
                    if (model is not null)
                    {
                        statements.Add(ddlGenerator.GenerateCreateTableSql(model, schema));
                    }
                    else
                    {
                        var classDef = FindClass(schema, tableDiff.TableName);
                        if (classDef is not null)
                            statements.Add(ddlGenerator.GenerateCreateTableFromClassSql(classDef, schema));
                    }

                    break;
                }
                case DdlDiff.TableRemoved:
                    statements.Add($"DROP TABLE IF EXISTS {_dialect.QuoteIdentifier(tableDiff.TableName)};");
                    break;
                case DdlDiff.TableModified:
                {
                    foreach (var colDiff in tableDiff.ColumnDiffs)
                        switch (colDiff.type)
                        {
                            case DdlDiff.ColumnAdded:
                                statements.Add($"ALTER TABLE {_dialect.QuoteIdentifier(tableDiff.TableName)} " +
                                               $"ADD COLUMN {_dialect.QuoteIdentifier(colDiff.ColumnName)} {colDiff.NewType ?? "TEXT"};");
                                break;
                            case DdlDiff.ColumnRemoved when _dialect.SupportsDropColumn:
                                statements.Add($"ALTER TABLE {_dialect.QuoteIdentifier(tableDiff.TableName)} " +
                                               $"DROP COLUMN {_dialect.QuoteIdentifier(colDiff.ColumnName)};");
                                break;
                            case DdlDiff.ColumnRemoved when !_dialect.SupportsDropColumn:
                                statements.Add(
                                    $"-- SQLite 不支持 DROP COLUMN: {tableDiff.TableName}.{colDiff.ColumnName}");
                                break;
                            case DdlDiff.ColumnModified:
                            {
                                var alterSql = _dialect.Name switch
                                {
                                    "mysql" => $"ALTER TABLE {_dialect.QuoteIdentifier(tableDiff.TableName)} " +
                                               $"MODIFY COLUMN {_dialect.QuoteIdentifier(colDiff.ColumnName)} {colDiff.NewType ?? "TEXT"};",
                                    "postgresql" => $"ALTER TABLE {_dialect.QuoteIdentifier(tableDiff.TableName)} " +
                                                    $"ALTER COLUMN {_dialect.QuoteIdentifier(colDiff.ColumnName)} TYPE {colDiff.NewType ?? "TEXT"};",
                                    _ => $"-- SQLite 不支持 ALTER COLUMN: {tableDiff.TableName}.{colDiff.ColumnName}"
                                };
                                statements.Add(alterSql);
                                break;
                            }
                        }

                    break;
                }
            }

        return statements;
    }

    private Dictionary<string, List<(string name, string type, bool isNullable, bool isPrimaryKey)>> GetSchemaTables(
        SchemaIR schema)
    {
        var tables = new Dictionary<string, List<(string, string, bool, bool)>>();

        foreach (var storage in schema.Storages)
        foreach (var model in storage.Models)
        {
            var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
            var columns = new List<(string, string, bool, bool)>();

            foreach (var field in model.fields)
            {
                if (field.IsFlatten && SqlTypeMapper.TryGetClassFields(field.FieldType, schema, out var flattenFields))
                {
                    var prefix = SqlTypeMapper.ToSnakeCase(field.Name) + "_";
                    foreach (var ff in flattenFields!)
                    {
                        var (colType, isNullable, _) = _mapper.ResolveFieldType(ff.FieldType, ff.IsOptional, schema);
                        columns.Add((prefix + SqlTypeMapper.ToSnakeCase(ff.Name), colType, isNullable,
                            ff.IsPrimaryKey));
                    }

                    continue;
                }

                var (fColType, fIsNullable, fkInfo) =
                    _mapper.ResolveFieldType(field.FieldType, field.IsOptional, schema);
                if (fkInfo is not null) fColType = fkInfo.colType;

                columns.Add((SqlTypeMapper.ToSnakeCase(field.Name), fColType, fIsNullable, field.IsPrimaryKey));
            }

            tables[tableName] = columns;
        }

        if (schema.Storages.Count == 0)
            foreach (var classDef in schema.Classes)
            {
                var tableName = SqlTypeMapper.ToSnakeCase(classDef.Name);
                var columns = new List<(string, string, bool, bool)>();

                foreach (var field in classDef.fields)
                {
                    var (colType, isNullable, _) = _mapper.ResolveFieldType(field.FieldType, field.IsOptional, schema);
                    columns.Add((SqlTypeMapper.ToSnakeCase(field.Name), colType, isNullable, field.IsPrimaryKey));
                }

                tables[tableName] = columns;
            }

        return tables;
    }

    private List<ColumnDiff> DiffColumns(string tableName,
        List<(string name, string type, bool isNullable, bool isPrimaryKey)> schemaColumns,
        List<ColumnInfo> dbColumns)
    {
        var diffs = new List<ColumnDiff>();
        var dbColMap = dbColumns.ToDictionary(c => c.Name, c => c);
        var schemaColNames = new HashSet<string>(schemaColumns.Select(c => c.name));

        foreach (var (name, type, isNullable, isPrimaryKey) in schemaColumns)
        {
            if (!dbColMap.TryGetValue(name, out var dbCol))
            {
                diffs.Add(new ColumnDiff
                {
                    Type = DdlDiff.ColumnAdded,
                    ColumnName = name,
                    NewType = type
                });
                continue;
            }

            var dbTypeNormalized = NormalizeType(dbCol.DataType);
            var schemaTypeNormalized = NormalizeType(type);

            if (dbTypeNormalized != schemaTypeNormalized)
                diffs.Add(new ColumnDiff
                {
                    Type = DdlDiff.ColumnModified,
                    ColumnName = name,
                    OldType = dbCol.DataType,
                    NewType = type
                });
        }

        foreach (var dbCol in dbColumns)
            if (!schemaColNames.Contains(dbCol.Name))
                diffs.Add(new ColumnDiff
                {
                    Type = DdlDiff.ColumnRemoved,
                    ColumnName = dbCol.Name,
                    OldType = dbCol.DataType
                });

        return diffs;
    }

    private static string NormalizeType(string type)
    {
        var t = type.ToUpperInvariant().Split('(', ')')[0].Trim();
        return t switch
        {
            "INT" => "INTEGER",
            "INTEGER" => "INTEGER",
            "VARCHAR" => "TEXT",
            "CHAR" => "TEXT",
            "BOOL" or "BOOLEAN" => "BOOLEAN",
            "FLOAT" or "REAL" => "FLOAT",
            "DOUBLE PRECISION" => "DOUBLE",
            "DATETIME" or "TIMESTAMP" or "TIMESTAMPTZ" => "DATETIME",
            "BIGINT UNSIGNED" => "BIGINT",
            "INT UNSIGNED" => "INTEGER",
            _ => t
        };
    }

    private ModelDefinition? FindModel(SchemaIR schema, string tableName)
    {
        foreach (var storage in schema.Storages)
        foreach (var model in storage.Models)
            if (SqlTypeMapper.ToSnakeCase(model.Name) == tableName)
                return model;

        return null;
    }

    private ClassDefinition? FindClass(SchemaIR schema, string tableName)
    {
        foreach (var classDef in schema.Classes)
            if (SqlTypeMapper.ToSnakeCase(classDef.Name) == tableName)
                return classDef;

        return null;
    }
}
using System.Text;
using Hermes.Generator;

namespace Hermes.Plugin.Sql.Dml;

/// <summary>
///     DML 生成器 — 根据 ModelDefinition 生成 INSERT / SELECT / UPDATE / DELETE 语句
/// </summary>
public sealed class DmlGenerator
{
    private readonly SqlDialect _dialect;
    private readonly SqlTypeMapper _mapper;

    public DmlGenerator(SqlDialect dialect)
    {
        _dialect = dialect;
        _mapper = new SqlTypeMapper(dialect);
    }

    /// <summary>
    ///     生成 INSERT 语句
    /// </summary>
    public string GenerateInsert(ModelDefinition model, SchemaIR schema)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        var sb = new StringBuilder();

        var columns = GetInsertableColumns(model, schema);
        var colNames = columns.Select(c => _dialect.QuoteIdentifier(c.columnName));
        var paramNames = columns.Select(c => $"@{c.columnName}");

        sb.AppendLine($"INSERT INTO {_dialect.QuoteIdentifier(tableName)}");
        sb.AppendLine($"    ({string.Join(", ", colNames)})");
        sb.AppendLine("VALUES");
        sb.AppendLine($"    ({string.Join(", ", paramNames)})");

        if (_dialect.SupportsReturning)
        {
            var pkField = model.fields.FirstOrDefault(f => f.IsPrimaryKey);
            if (pkField is not null)
                sb.AppendLine($"RETURNING {_dialect.QuoteIdentifier(SqlTypeMapper.ToSnakeCase(pkField.Name))}");
        }

        sb.Append(';');
        return sb.ToString();
    }

    /// <summary>
    ///     生成 UPSERT 语句（INSERT ON CONFLICT / ON DUPLICATE KEY UPDATE）
    /// </summary>
    public string GenerateUpsert(ModelDefinition model, SchemaIR schema)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        var columns = GetInsertableColumns(model, schema);
        var colNames = columns.Select(c => _dialect.QuoteIdentifier(c.columnName));
        var paramNames = columns.Select(c => $"@{c.columnName}");

        var sb = new StringBuilder();
        sb.AppendLine($"INSERT INTO {_dialect.QuoteIdentifier(tableName)}");
        sb.AppendLine($"    ({string.Join(", ", colNames)})");
        sb.AppendLine("VALUES");
        sb.AppendLine($"    ({string.Join(", ", paramNames)})");

        var nonKeyColumns = columns.Where(c => !c.isPrimaryKey).ToList();
        if (nonKeyColumns.Count > 0)
        {
            if (_dialect.Name is "mysql")
            {
                var updates = nonKeyColumns.Select(c =>
                    $"{_dialect.QuoteIdentifier(c.columnName)} = VALUES({_dialect.QuoteIdentifier(c.columnName)})");
                sb.AppendLine("ON DUPLICATE KEY UPDATE");
                sb.AppendLine($"    {string.Join(", ", updates)}");
            }
            else if (_dialect.Name is "postgresql")
            {
                var pkCol = columns.FirstOrDefault(c => c.isPrimaryKey);
                if (pkCol != default)
                {
                    var conflictCol = _dialect.QuoteIdentifier(pkCol.columnName);
                    var updates = nonKeyColumns.Select(c =>
                        $"{_dialect.QuoteIdentifier(c.columnName)} = EXCLUDED.{_dialect.QuoteIdentifier(c.columnName)}");
                    sb.AppendLine($"ON CONFLICT ({conflictCol})");
                    sb.AppendLine("DO UPDATE SET");
                    sb.AppendLine($"    {string.Join(", ", updates)}");
                }
            }
            else if (_dialect.Name is "sqlite")
            {
                var updates = nonKeyColumns.Select(c =>
                    $"{_dialect.QuoteIdentifier(c.columnName)} = excluded.{_dialect.QuoteIdentifier(c.columnName)}");
                sb.AppendLine("ON CONFLICT DO UPDATE SET");
                sb.AppendLine($"    {string.Join(", ", updates)}");
            }
        }

        sb.Append(';');
        return sb.ToString();
    }

    /// <summary>
    ///     生成 SELECT BY ID 语句
    /// </summary>
    public string GenerateSelectById(ModelDefinition model, SchemaIR schema)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        var pkField = model.fields.FirstOrDefault(f => f.IsPrimaryKey) ?? model.fields.FirstOrDefault();
        if (pkField is null) return $"-- Model {model.Name} has no fields";

        var pkCol = SqlTypeMapper.ToSnakeCase(pkField.Name);
        var selectCols = GetSelectColumns(model, schema);

        return
            $"SELECT {selectCols} FROM {_dialect.QuoteIdentifier(tableName)} WHERE {_dialect.QuoteIdentifier(pkCol)} = @{pkCol};";
    }

    /// <summary>
    ///     生成 SELECT ALL 语句
    /// </summary>
    public string GenerateSelectAll(ModelDefinition model, SchemaIR schema)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        var selectCols = GetSelectColumns(model, schema);
        return $"SELECT {selectCols} FROM {_dialect.QuoteIdentifier(tableName)};";
    }

    /// <summary>
    ///     生成 SELECT WHERE 语句
    /// </summary>
    public string GenerateSelectWhere(ModelDefinition model, string whereClause)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        return $"SELECT * FROM {_dialect.QuoteIdentifier(tableName)} WHERE {whereClause};";
    }

    /// <summary>
    ///     生成 UPDATE 语句
    /// </summary>
    public string GenerateUpdate(ModelDefinition model, SchemaIR schema)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        var pkField = model.fields.FirstOrDefault(f => f.IsPrimaryKey) ?? model.fields.FirstOrDefault();
        if (pkField is null) return $"-- Model {model.Name} has no fields";

        var pkCol = SqlTypeMapper.ToSnakeCase(pkField.Name);
        var updatableColumns = GetInsertableColumns(model, schema)
            .Where(c => !c.isPrimaryKey)
            .ToList();

        if (updatableColumns.Count == 0) return $"-- Model {model.Name} has no updatable columns";

        var sets = updatableColumns.Select(c =>
            $"{_dialect.QuoteIdentifier(c.columnName)} = @{c.columnName}");

        var sb = new StringBuilder();
        sb.AppendLine($"UPDATE {_dialect.QuoteIdentifier(tableName)}");
        sb.AppendLine($"SET {string.Join(", ", sets)}");
        sb.AppendLine($"WHERE {_dialect.QuoteIdentifier(pkCol)} = @{pkCol};");

        return sb.ToString();
    }

    /// <summary>
    ///     生成 DELETE 语句
    /// </summary>
    public string GenerateDelete(ModelDefinition model)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        var pkField = model.fields.FirstOrDefault(f => f.IsPrimaryKey) ?? model.fields.FirstOrDefault();
        if (pkField is null) return $"-- Model {model.Name} has no fields";

        var pkCol = SqlTypeMapper.ToSnakeCase(pkField.Name);
        return $"DELETE FROM {_dialect.QuoteIdentifier(tableName)} WHERE {_dialect.QuoteIdentifier(pkCol)} = @{pkCol};";
    }

    /// <summary>
    ///     生成 COUNT 语句
    /// </summary>
    public string GenerateCount(ModelDefinition model)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        return $"SELECT COUNT(*) FROM {_dialect.QuoteIdentifier(tableName)};";
    }

    /// <summary>
    ///     生成 EXISTS 语句
    /// </summary>
    public string GenerateExists(ModelDefinition model)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        var pkField = model.fields.FirstOrDefault(f => f.IsPrimaryKey) ?? model.fields.FirstOrDefault();
        if (pkField is null) return $"-- Model {model.Name} has no fields";

        var pkCol = SqlTypeMapper.ToSnakeCase(pkField.Name);
        return
            $"SELECT EXISTS(SELECT 1 FROM {_dialect.QuoteIdentifier(tableName)} WHERE {_dialect.QuoteIdentifier(pkCol)} = @{pkCol});";
    }

    /// <summary>
    ///     生成所有 DML 语句文件
    /// </summary>
    public GeneratedFile GenerateDmlFile(ModelDefinition model, SchemaIR schema, string outputPath, string schemaPath)
    {
        var tableName = SqlTypeMapper.ToSnakeCase(model.Name);
        var sb = new StringBuilder();

        sb.AppendLine("-- <auto-generated>");
        sb.AppendLine("-- 本文件由 atlas/hermes 命令自动生成，修改无效");
        sb.AppendLine($"-- 源文件: {schemaPath}");
        sb.AppendLine($"-- Model: {model.Name}");
        sb.AppendLine("-- </auto-generated>");
        sb.AppendLine();

        sb.AppendLine("-- INSERT");
        sb.AppendLine(GenerateInsert(model, schema));
        sb.AppendLine();

        if (_dialect.SupportsUpsert)
        {
            sb.AppendLine("-- UPSERT");
            sb.AppendLine(GenerateUpsert(model, schema));
            sb.AppendLine();
        }

        sb.AppendLine("-- SELECT BY ID");
        sb.AppendLine(GenerateSelectById(model, schema));
        sb.AppendLine();

        sb.AppendLine("-- SELECT ALL");
        sb.AppendLine(GenerateSelectAll(model, schema));
        sb.AppendLine();

        sb.AppendLine("-- UPDATE");
        sb.AppendLine(GenerateUpdate(model, schema));
        sb.AppendLine();

        sb.AppendLine("-- DELETE");
        sb.AppendLine(GenerateDelete(model));
        sb.AppendLine();

        sb.AppendLine("-- COUNT");
        sb.AppendLine(GenerateCount(model));
        sb.AppendLine();

        sb.AppendLine("-- EXISTS");
        sb.AppendLine(GenerateExists(model));

        return new GeneratedFile
        {
            Path = Path.Combine(outputPath, $"{tableName}_dml.sql"),
            Content = sb.ToString(),
            Generator = "sql-dml"
        };
    }

    private List<(string columnName, bool isPrimaryKey)> GetInsertableColumns(ModelDefinition model, SchemaIR schema)
    {
        var result = new List<(string, bool)>();

        foreach (var field in model.fields)
        {
            if (field.IsFlatten && SqlTypeMapper.TryGetClassFields(field.FieldType, schema, out var flattenFields))
            {
                var prefix = SqlTypeMapper.ToSnakeCase(field.Name) + "_";
                foreach (var ff in flattenFields!)
                {
                    var ffColName = prefix + SqlTypeMapper.ToSnakeCase(ff.Name);
                    result.Add((ffColName, ff.IsPrimaryKey));
                }

                continue;
            }

            var colName = SqlTypeMapper.ToSnakeCase(field.Name);
            result.Add((colName, field.IsPrimaryKey));
        }

        return result;
    }

    private string GetSelectColumns(ModelDefinition model, SchemaIR schema)
    {
        var columns = new List<string>();

        foreach (var field in model.fields)
        {
            if (field.IsFlatten && SqlTypeMapper.TryGetClassFields(field.FieldType, schema, out var flattenFields))
            {
                var prefix = SqlTypeMapper.ToSnakeCase(field.Name) + "_";
                foreach (var ff in flattenFields!)
                    columns.Add(_dialect.QuoteIdentifier(prefix + SqlTypeMapper.ToSnakeCase(ff.Name)));
                continue;
            }

            columns.Add(_dialect.QuoteIdentifier(SqlTypeMapper.ToSnakeCase(field.Name)));
        }

        return string.Join(", ", columns);
    }
}
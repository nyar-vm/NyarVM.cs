using System.Text;

namespace Hermes.Plugin.Sql;

/// <summary>
///     Schema 类型到 SQL 类型的映射器
/// </summary>
public sealed class SqlTypeMapper
{
    private readonly SqlDialect _dialect;

    public SqlTypeMapper(SqlDialect dialect)
    {
        _dialect = dialect;
    }

    /// <summary>
    ///     将 SchemaType 映射为 SQL 列类型
    /// </summary>
    public string MapSqlType(SchemaType type)
    {
        return type switch
        {
            PrimitiveType p => p.TypeName switch
            {
                "i8" => _dialect.Name is "postgresql" ? "SMALLINT" : _dialect.Name is "sqlite" ? "INTEGER" : "TINYINT",
                "i16" => "SMALLINT",
                "i32" => "INTEGER",
                "i64" => "BIGINT",
                "u8" => _dialect.Name is "postgresql" ? "SMALLINT" :
                    _dialect.Name is "sqlite" ? "INTEGER" : "TINYINT UNSIGNED",
                "u16" => _dialect.Name is "postgresql" ? "INT" :
                    _dialect.Name is "sqlite" ? "INTEGER" : "SMALLINT UNSIGNED",
                "u32" => _dialect.Name is "postgresql" ? "BIGINT" :
                    _dialect.Name is "sqlite" ? "INTEGER" : "INT UNSIGNED",
                "u64" => _dialect.Name is "postgresql" ? "BIGINT" :
                    _dialect.Name is "sqlite" ? "INTEGER" : "BIGINT UNSIGNED",
                "f32" => _dialect.Name is "sqlite" ? "REAL" : "FLOAT",
                "f64" => _dialect.Name is "sqlite" ? "REAL" : "DOUBLE PRECISION",
                "bool" => _dialect.Name is "sqlite" ? "INTEGER" : "BOOLEAN",
                "utf8" => "TEXT",
                "utf16" => "TEXT",
                "uuid" => _dialect.Name is "postgresql" ? "UUID" : _dialect.Name is "sqlite" ? "TEXT" : "CHAR(36)",
                "unit" => "BOOLEAN",
                "object" => _dialect.Name is "postgresql" ? "JSONB" : "TEXT",
                "datetime" => _dialect.Name is "sqlite" ? "TEXT" :
                    _dialect.Name is "postgresql" ? "TIMESTAMPTZ" : "DATETIME",
                "decimal" => "DECIMAL(18,2)",
                _ => "TEXT"
            },
            ListType => _dialect.Name is "postgresql" ? "JSONB" : "TEXT",
            DictType => _dialect.Name is "postgresql" ? "JSONB" : "TEXT",
            RecordType => _dialect.Name is "postgresql" ? "JSONB" : "TEXT",
            OptionType o => MapSqlType(o.InnerType),
            ArrayType => _dialect.Name is "postgresql" ? "JSONB" : "TEXT",
            ResultType => _dialect.Name is "postgresql" ? "JSONB" : "TEXT",
            ReferenceType refT => MapSqlType(refT.ReferencedType),
            NamedType n => n.Name switch
            {
                "datetime" => _dialect.Name is "sqlite" ? "TEXT" :
                    _dialect.Name is "postgresql" ? "TIMESTAMPTZ" : "DATETIME",
                "map" => _dialect.Name is "postgresql" ? "JSONB" : "TEXT",
                "set" => _dialect.Name is "postgresql" ? "JSONB" : "TEXT",
                "bytes" => _dialect.Name is "postgresql" ? "BYTEA" : "BLOB",
                "decimal" => "DECIMAL(18,2)",
                "any" => _dialect.Name is "postgresql" ? "JSONB" : "TEXT",
                _ => "TEXT"
            },
            _ => "TEXT"
        };
    }

    /// <summary>
    ///     解析字段类型，返回列类型、是否可空、外键信息
    /// </summary>
    public (string colType, bool isNullable, ForeignKeyInfo? fkInfo) ResolveFieldType(
        SchemaType type, bool isOptional, SchemaIR schema)
    {
        switch (type)
        {
            case OptionType opt:
            {
                var inner = ResolveFieldType(opt.InnerType, true, schema);
                return (inner.colType, true, inner.fkInfo);
            }
            case ReferenceType refType:
            {
                var refTypeName = GetReferencedTypeName(refType);
                var fkInfo = ResolveForeignKey(refType, schema);
                return (fkInfo?.colType ?? MapSqlType(refType.ReferencedType), isOptional, fkInfo);
            }
            case ListType listType when listType.ElementType is ReferenceType:
            {
                return (_dialect.Name is "postgresql" ? "UUID[]" : "TEXT", isOptional, null);
            }
            case NamedType named when IsEnumType(named.Name, schema):
                return ("SMALLINT", isOptional, null);
            case NamedType named when IsInlineType(named.Name, schema):
                return (_dialect.Name is "postgresql" ? "JSONB" : "TEXT", isOptional, null);
            default:
                return (MapSqlType(type), isOptional, null);
        }
    }

    /// <summary>
    ///     解析外键信息
    /// </summary>
    public ForeignKeyInfo? ResolveForeignKey(ReferenceType refType, SchemaIR schema)
    {
        var refTypeName = GetReferencedTypeName(refType);

        foreach (var storage in schema.Storages)
        foreach (var model in storage.Models)
            if (model.Name == refTypeName)
            {
                var keyField = model.fields.FirstOrDefault(f => f.IsPrimaryKey);
                var keyType = keyField != null ? MapSqlType(keyField.FieldType) : MapSqlType(model.KeyType);
                var keyName = keyField?.Name ?? "id";
                return new ForeignKeyInfo(keyType, ToSnakeCase(model.Name), ToSnakeCase(keyName));
            }

        foreach (var classDef in schema.Classes)
            if (classDef.Name == refTypeName)
            {
                var keyField = classDef.fields.FirstOrDefault(f => f.IsPrimaryKey);
                if (keyField != null)
                    return new ForeignKeyInfo(MapSqlType(keyField.FieldType), ToSnakeCase(classDef.Name),
                        ToSnakeCase(keyField.Name));
            }

        return null;
    }

    /// <summary>
    ///     将 SQL 类型映射回 Hermes 类型
    /// </summary>
    public static string MapFromSqlType(string sqlType)
    {
        var upper = sqlType.ToUpperInvariant().Split('(', ')')[0].Trim();
        return upper switch
        {
            "INTEGER" or "INT" => "i32",
            "BIGINT" => "i64",
            "SMALLINT" => "i16",
            "TINYINT" => "i8",
            "REAL" or "FLOAT" => "f32",
            "DOUBLE" or "DOUBLE PRECISION" => "f64",
            "BOOLEAN" => "bool",
            "TEXT" or "VARCHAR" or "CHAR" or "CHARACTER" => "utf8",
            "UUID" => "uuid",
            "BLOB" => "list<u8>",
            "DATETIME" or "TIMESTAMP" or "DATE" or "TIMESTAMPTZ" => "datetime",
            "JSONB" or "JSON" => "map<utf8, utf8>",
            _ => "utf8"
        };
    }

    public static bool IsEnumType(string name, SchemaIR schema)
    {
        return schema.Enums.Any(e => e.Name == name);
    }

    public static bool IsInlineType(string name, SchemaIR schema)
    {
        return schema.Unions.Any(u => u.Name == name)
               || schema.Classes.Any(c => c.Name == name)
               || schema.Flags.Any(f => f.Name == name);
    }

    public static bool TryGetClassFields(SchemaType type, SchemaIR schema, out IReadOnlyList<FieldDefinition>? fields)
    {
        fields = null;
        if (type is not NamedType named) return false;

        var classDef = schema.Classes.FirstOrDefault(c => c.Name == named.Name);
        if (classDef is null) return false;

        fields = classDef.fields;
        return true;
    }

    public static string GetReferencedTypeName(ReferenceType refType)
    {
        return refType.ReferencedType switch
        {
            NamedType n => n.Name,
            PrimitiveType p => p.TypeName,
            _ => refType.ReferencedType.ToString() ?? "unknown"
        };
    }

    public static string ToSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var sb = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0) sb.Append('_');

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}

/// <summary>
///     外键信息
/// </summary>
public sealed class ForeignKeyInfo
{
    public ForeignKeyInfo(string colType, string refTableName, string refKeyName)
    {
        this.colType = colType;
        this.refTableName = refTableName;
        this.refKeyName = refKeyName;
    }

    public string colType { get; }
    public string refTableName { get; }
    public string refKeyName { get; }
}
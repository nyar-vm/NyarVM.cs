namespace Hermes.Hosting;

/// <summary>
///     数据库方言工厂——根据连接字符串自动检测方言并创建对应的翻译器
/// </summary>
public sealed class DatabaseDialectFactory
{
    /// <summary>
    ///     根据连接字符串自动检测方言
    /// </summary>
    public static IDatabaseDialect Detect(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString)) throw new ArgumentException("连接字符串不能为空", nameof(connectionString));

        var lower = connectionString.ToLowerInvariant();

        if (lower.Contains("postgresql") || lower.Contains("postgres://") ||
            (lower.Contains("host=") && lower.Contains("port=5432"))) return new PostgreSqlDialect();

        if (lower.Contains("mysql") || (lower.Contains("server=") && lower.Contains("port=3306")))
            return new MySqlDialect();

        if (lower.Contains("sqlite") || lower.Contains(".db") || lower.Contains(".sqlite") ||
            lower.Contains(":memory:")) return new SqliteDialect();

        throw new NotSupportedException($"无法识别的数据库连接字符串：{connectionString}");
    }

    /// <summary>
    ///     根据方言名称创建方言实例
    /// </summary>
    public static IDatabaseDialect Create(string dialectName)
    {
        return dialectName.ToLowerInvariant() switch
        {
            "mysql" => new MySqlDialect(),
            "postgresql" or "pgsql" => new PostgreSqlDialect(),
            "sqlite" => new SqliteDialect(),
            _ => throw new NotSupportedException($"不支持的数据库方言：{dialectName}")
        };
    }

    #region MySQL 方言

    private sealed class MySqlDialect : IDatabaseDialect
    {
        private readonly MySqlQueryTranslator _translator = new();

        /// <inheritdoc />
        public string Name => "mysql";

        /// <inheritdoc />
        public string QuoteChar => "`";

        /// <inheritdoc />
        public bool SupportsReturning => false;

        /// <inheritdoc />
        public bool SupportsCte => true;

        /// <inheritdoc />
        public bool SupportsWindowFunctions => true;

        /// <inheritdoc />
        public bool SupportsFullTextSearch => true;

        /// <inheritdoc />
        public bool SupportsUpsert => true;

        /// <inheritdoc />
        public bool SupportsRecursiveCte => true;

        /// <inheritdoc />
        public UpsertSyntaxKind UpsertSyntax => UpsertSyntaxKind.OnDuplicateKey;

        /// <inheritdoc />
        public string Translate(QueryExpression query)
        {
            return _translator.Translate(query);
        }

        /// <inheritdoc />
        public string QuoteIdentifier(string identifier)
        {
            return $"`{identifier}`";
        }

        /// <inheritdoc />
        public string FormatValue(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s.Replace("\\", "\\\\").Replace("'", "\\'")}'",
                bool b => b ? "1" : "0",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                byte[] bytes => $"0x{Convert.ToHexString(bytes)}",
                _ => value.ToString() ?? "NULL"
            };
        }

        /// <inheritdoc />
        public string GetAutoIncrementDefinition()
        {
            return "AUTO_INCREMENT";
        }

        /// <inheritdoc />
        public string GetPaginationClause(int offset, int limit)
        {
            return offset > 0 ? $"LIMIT {offset}, {limit}" : $"LIMIT {limit}";
        }
    }

    #endregion

    #region PostgreSQL 方言

    private sealed class PostgreSqlDialect : IDatabaseDialect
    {
        private readonly PostgreSqlQueryTranslator _translator = new();

        /// <inheritdoc />
        public string Name => "postgresql";

        /// <inheritdoc />
        public string QuoteChar => "\"";

        /// <inheritdoc />
        public bool SupportsReturning => true;

        /// <inheritdoc />
        public bool SupportsCte => true;

        /// <inheritdoc />
        public bool SupportsWindowFunctions => true;

        /// <inheritdoc />
        public bool SupportsFullTextSearch => true;

        /// <inheritdoc />
        public bool SupportsUpsert => true;

        /// <inheritdoc />
        public bool SupportsRecursiveCte => true;

        /// <inheritdoc />
        public UpsertSyntaxKind UpsertSyntax => UpsertSyntaxKind.OnConflict;

        /// <inheritdoc />
        public string Translate(QueryExpression query)
        {
            return _translator.Translate(query);
        }

        /// <inheritdoc />
        public string QuoteIdentifier(string identifier)
        {
            return $"\"{identifier}\"";
        }

        /// <inheritdoc />
        public string FormatValue(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s.Replace("'", "''")}'",
                bool b => b ? "TRUE" : "FALSE",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                byte[] bytes => $"\\x{Convert.ToHexString(bytes).ToLowerInvariant()}",
                _ => value.ToString() ?? "NULL"
            };
        }

        /// <inheritdoc />
        public string GetAutoIncrementDefinition()
        {
            return "GENERATED ALWAYS AS IDENTITY";
        }

        /// <inheritdoc />
        public string GetPaginationClause(int offset, int limit)
        {
            return offset > 0 ? $"OFFSET {offset} LIMIT {limit}" : $"LIMIT {limit}";
        }
    }

    #endregion

    #region SQLite 方言

    private sealed class SqliteDialect : IDatabaseDialect
    {
        private readonly SqliteQueryTranslator _translator = new();

        /// <inheritdoc />
        public string Name => "sqlite";

        /// <inheritdoc />
        public string QuoteChar => "\"";

        /// <inheritdoc />
        public bool SupportsReturning => false;

        /// <inheritdoc />
        public bool SupportsCte => true;

        /// <inheritdoc />
        public bool SupportsWindowFunctions => true;

        /// <inheritdoc />
        public bool SupportsFullTextSearch => false;

        /// <inheritdoc />
        public bool SupportsUpsert => true;

        /// <inheritdoc />
        public bool SupportsRecursiveCte => true;

        /// <inheritdoc />
        public UpsertSyntaxKind UpsertSyntax => UpsertSyntaxKind.InsertOrReplace;

        /// <inheritdoc />
        public string Translate(QueryExpression query)
        {
            return _translator.Translate(query);
        }

        /// <inheritdoc />
        public string QuoteIdentifier(string identifier)
        {
            return $"\"{identifier}\"";
        }

        /// <inheritdoc />
        public string FormatValue(object? value)
        {
            return value switch
            {
                null => "NULL",
                string s => $"'{s.Replace("'", "''")}'",
                bool b => b ? "1" : "0",
                DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
                byte[] bytes => $"x'{Convert.ToHexString(bytes)}'",
                _ => value.ToString() ?? "NULL"
            };
        }

        /// <inheritdoc />
        public string GetAutoIncrementDefinition()
        {
            return "AUTOINCREMENT";
        }

        /// <inheritdoc />
        public string GetPaginationClause(int offset, int limit)
        {
            return offset > 0 ? $"LIMIT {limit} OFFSET {offset}" : $"LIMIT {limit}";
        }
    }

    #endregion
}
namespace Hermes.Plugin.Sql;

/// <summary>
///     SQL 方言抽象，封装不同数据库的语法差异
/// </summary>
public sealed class SqlDialect
{
    private SqlDialect(string name, string quoteLeft, string quoteRight,
        bool supportsCreateDatabase, bool supportsAlterColumn, bool supportsDropColumn,
        bool supportsReturning, bool supportsUpsert, string autoIncrementSyntax,
        string booleanTrue, string booleanFalse)
    {
        Name = name;
        QuoteLeft = quoteLeft;
        QuoteRight = quoteRight;
        SupportsCreateDatabase = supportsCreateDatabase;
        SupportsAlterColumn = supportsAlterColumn;
        SupportsDropColumn = supportsDropColumn;
        SupportsReturning = supportsReturning;
        SupportsUpsert = supportsUpsert;
        AutoIncrementSyntax = autoIncrementSyntax;
        BooleanTrue = booleanTrue;
        BooleanFalse = booleanFalse;
    }

    /// <summary>
    ///     方言名称：mysql / postgresql / sqlite
    /// </summary>
    public string Name { get; }

    /// <summary>
    ///     标识符引用符（左）
    /// </summary>
    public string QuoteLeft { get; }

    /// <summary>
    ///     标识符引用符（右）
    /// </summary>
    public string QuoteRight { get; }

    /// <summary>
    ///     是否支持 CREATE DATABASE
    /// </summary>
    public bool SupportsCreateDatabase { get; }

    /// <summary>
    ///     是否支持 ALTER COLUMN
    /// </summary>
    public bool SupportsAlterColumn { get; }

    /// <summary>
    ///     是否支持 DROP COLUMN
    /// </summary>
    public bool SupportsDropColumn { get; }

    /// <summary>
    ///     是否支持 INSERT ... RETURNING
    /// </summary>
    public bool SupportsReturning { get; }

    /// <summary>
    ///     是否支持 UPSERT (ON CONFLICT / ON DUPLICATE KEY)
    /// </summary>
    public bool SupportsUpsert { get; }

    /// <summary>
    ///     自增主键语法
    /// </summary>
    public string AutoIncrementSyntax { get; }

    /// <summary>
    ///     布尔真值
    /// </summary>
    public string BooleanTrue { get; }

    /// <summary>
    ///     布尔假值
    /// </summary>
    public string BooleanFalse { get; }

    /// <summary>
    ///     MySQL 方言
    /// </summary>
    public static SqlDialect MySql => new("mysql", "`", "`",
        true, true, true, false, true, "AUTO_INCREMENT", "1", "0");

    /// <summary>
    ///     PostgreSQL 方言
    /// </summary>
    public static SqlDialect PostgreSql => new("postgresql", "\"", "\"",
        true, true, true, true, true, "SERIAL", "TRUE", "FALSE");

    /// <summary>
    ///     SQLite 方言
    /// </summary>
    public static SqlDialect Sqlite => new("sqlite", "\"", "\"",
        false, false, false, false, true, "AUTOINCREMENT", "1", "0");

    /// <summary>
    ///     根据名称获取方言
    /// </summary>
    public static SqlDialect FromName(string name)
    {
        return name.ToLowerInvariant() switch
        {
            "mysql" => MySql,
            "postgresql" or "pgsql" => PostgreSql,
            "sqlite" => Sqlite,
            _ => throw new ArgumentException($"不支持的 SQL 方言: {name}")
        };
    }

    /// <summary>
    ///     引用标识符
    /// </summary>
    public string QuoteIdentifier(string identifier)
    {
        return $"{QuoteLeft}{identifier}{QuoteRight}";
    }

    /// <summary>
    ///     格式化布尔值
    /// </summary>
    public string FormatBool(bool value)
    {
        return value ? BooleanTrue : BooleanFalse;
    }

    /// <summary>
    ///     格式化字符串值
    /// </summary>
    public string FormatString(string value)
    {
        return $"'{value.Replace("'", "''")}'";
    }

    /// <summary>
    ///     格式化默认值
    /// </summary>
    public string FormatDefaultValue(object value)
    {
        if (value is string s) return FormatString(s);

        if (value is bool b) return FormatBool(b);

        return value.ToString() ?? "NULL";
    }
}
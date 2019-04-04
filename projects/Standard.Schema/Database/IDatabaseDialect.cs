using Hermes.YYDB.Query;

namespace Hermes.Database;

/// <summary>
///     数据库方言接口——统一不同数据库的 SQL 方言差异
/// </summary>
public interface IDatabaseDialect
{
    /// <summary>
    ///     方言名称（mysql/postgresql/sqlite）
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     标识符引用字符（MySQL 反引号、PostgreSQL/SQLite 双引号）
    /// </summary>
    string QuoteChar { get; }

    /// <summary>
    ///     是否支持 RETURNING 子句
    /// </summary>
    bool SupportsReturning { get; }

    /// <summary>
    ///     是否支持 CTE（WITH ... AS ...）
    /// </summary>
    bool SupportsCte { get; }

    /// <summary>
    ///     是否支持窗口函数
    /// </summary>
    bool SupportsWindowFunctions { get; }

    /// <summary>
    ///     是否支持全文搜索
    /// </summary>
    bool SupportsFullTextSearch { get; }

    /// <summary>
    ///     是否支持 UPSERT
    /// </summary>
    bool SupportsUpsert { get; }

    /// <summary>
    ///     是否支持递归 CTE
    /// </summary>
    bool SupportsRecursiveCte { get; }

    /// <summary>
    ///     获取 UPSERT 语法类型
    /// </summary>
    UpsertSyntaxKind UpsertSyntax { get; }

    /// <summary>
    ///     将查询表达式翻译为当前方言的 SQL 语句
    /// </summary>
    string Translate(QueryExpression query);

    /// <summary>
    ///     引用标识符
    /// </summary>
    string QuoteIdentifier(string identifier);

    /// <summary>
    ///     格式化值
    /// </summary>
    string FormatValue(object? value);

    /// <summary>
    ///     获取自动递增列定义
    /// </summary>
    string GetAutoIncrementDefinition();

    /// <summary>
    ///     获取分页子句
    /// </summary>
    string GetPaginationClause(int offset, int limit);
}

/// <summary>
///     UPSERT 语法类型
/// </summary>
public enum UpsertSyntaxKind
{
    /// <summary>
    ///     不支持 UPSERT
    /// </summary>
    None,

    /// <summary>
    ///     PostgreSQL: ON CONFLICT ... DO UPDATE
    /// </summary>
    OnConflict,

    /// <summary>
    ///     MySQL: ON DUPLICATE KEY UPDATE
    /// </summary>
    OnDuplicateKey,

    /// <summary>
    ///     SQLite: INSERT OR REPLACE / INSERT OR IGNORE
    /// </summary>
    InsertOrReplace,

    /// <summary>
    ///     MySQL: REPLACE INTO
    /// </summary>
    ReplaceInto
}
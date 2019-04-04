using Hermes.Database.MySql;
using Hermes.Database.PostgreSql;
using Hermes.Database.Sqlite;
using Xunit;

namespace Hermes.ORM.Tests;

/// <summary>
///     跨方言集成测试——验证同一 QueryExpression 在 MySQL/PostgreSQL/SQLite 三方言下生成正确的 SQL
/// </summary>
public sealed class CrossDialectTests
{
    private readonly MySqlQueryTranslator _mysql = new();
    private readonly PostgreSqlQueryTranslator _pgsql = new();
    private readonly SqliteQueryTranslator _sqlite = new();

    #region 窗口函数——三方言共享语法

    [Fact]
    public void WindowQuery_AllDialectsSupportRowNumber()
    {
        var query = new WindowQuery("employees",
        [
            new WindowFunctionDefinition(
                WindowFunctionKind.RowNumber,
                "salary",
                "row_num",
                partitionBy: ["department"],
                orderBy: [new WindowOrderItem("salary", true)])
        ]);

        var mysqlSql = _mysql.Translate(query);
        var pgsqlSql = _pgsql.Translate(query);
        var sqliteSql = _sqlite.Translate(query);

        Assert.Contains("ROW_NUMBER()", mysqlSql);
        Assert.Contains("ROW_NUMBER()", pgsqlSql);
        Assert.Contains("ROW_NUMBER()", sqliteSql);

        Assert.Contains("PARTITION BY", mysqlSql);
        Assert.Contains("PARTITION BY", pgsqlSql);
        Assert.Contains("PARTITION BY", sqliteSql);
    }

    #endregion

    #region Find 查询——分页语法差异

    [Fact]
    public void FindQuery_Pagination_MySqlUsesLimitOffset()
    {
        var query = new FindQuery("users", null, null, new QueryPagination(10, 5));
        var sql = _mysql.Translate(query);
        Assert.Contains("LIMIT 10, 5", sql);
    }

    [Fact]
    public void FindQuery_Pagination_PostgreSqlUsesOffsetLimit()
    {
        var query = new FindQuery("users", null, null, new QueryPagination(10, 5));
        var sql = _pgsql.Translate(query);
        Assert.Contains("OFFSET 10", sql);
        Assert.Contains("LIMIT 5", sql);
    }

    [Fact]
    public void FindQuery_Pagination_SqliteUsesLimitOffset()
    {
        var query = new FindQuery("users", null, null, new QueryPagination(10, 5));
        var sql = _sqlite.Translate(query);
        Assert.Contains("LIMIT 5", sql);
        Assert.Contains("OFFSET 10", sql);
    }

    #endregion

    #region 标识符引用差异

    [Fact]
    public void FindQuery_IdentifierQuote_MySqlUsesBacktick()
    {
        var query = new FindQuery("users", new FieldEquals("name", "test"), null, null);
        var sql = _mysql.Translate(query);
        Assert.Contains("`users`", sql);
        Assert.Contains("`name`", sql);
    }

    [Fact]
    public void FindQuery_IdentifierQuote_PostgreSqlUsesDoubleQuote()
    {
        var query = new FindQuery("users", new FieldEquals("name", "test"), null, null);
        var sql = _pgsql.Translate(query);
        Assert.Contains("\"users\"", sql);
        Assert.Contains("\"name\"", sql);
    }

    [Fact]
    public void FindQuery_IdentifierQuote_SqliteUsesDoubleQuote()
    {
        var query = new FindQuery("users", new FieldEquals("name", "test"), null, null);
        var sql = _sqlite.Translate(query);
        Assert.Contains("\"users\"", sql);
        Assert.Contains("\"name\"", sql);
    }

    #endregion

    #region 布尔值格式化差异

    [Fact]
    public void FindQuery_BooleanValue_MySqlUses01()
    {
        var query = new FindQuery("users", new FieldEquals("active", true), null, null);
        var sql = _mysql.Translate(query);
        Assert.Contains("= 1", sql);
    }

    [Fact]
    public void FindQuery_BooleanValue_PostgreSqlUsesTrueFalse()
    {
        var query = new FindQuery("users", new FieldEquals("active", true), null, null);
        var sql = _pgsql.Translate(query);
        Assert.Contains("= TRUE", sql);
    }

    [Fact]
    public void FindQuery_BooleanValue_SqliteUses01()
    {
        var query = new FindQuery("users", new FieldEquals("active", true), null, null);
        var sql = _sqlite.Translate(query);
        Assert.Contains("= 1", sql);
    }

    #endregion

    #region UPSERT 语法差异

    [Fact]
    public void UpsertQuery_MySqlUsesOnDuplicateKey()
    {
        var query = new UpsertQuery(
            "users",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "张三")],
            ["id"],
            UpsertStrategy.DoUpdate);
        var sql = _mysql.Translate(query);
        Assert.Contains("ON DUPLICATE KEY UPDATE", sql);
        Assert.Contains("VALUES(", sql);
    }

    [Fact]
    public void UpsertQuery_PostgreSqlUsesOnConflict()
    {
        var query = new UpsertQuery(
            "users",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "张三")],
            ["id"],
            UpsertStrategy.DoUpdate);
        var sql = _pgsql.Translate(query);
        Assert.Contains("ON CONFLICT", sql);
        Assert.Contains("DO UPDATE SET", sql);
        Assert.Contains("EXCLUDED", sql);
        Assert.Contains("RETURNING *", sql);
    }

    [Fact]
    public void UpsertQuery_SqliteUsesInsertOrReplace()
    {
        var query = new UpsertQuery(
            "users",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "张三")],
            ["id"],
            UpsertStrategy.DoUpdate);
        var sql = _sqlite.Translate(query);
        Assert.Contains("INSERT OR REPLACE INTO", sql);
    }

    [Fact]
    public void UpsertQuery_DoNothing_MySqlUsesOnDuplicateKeyNoop()
    {
        var query = new UpsertQuery(
            "users",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "张三")],
            ["id"],
            UpsertStrategy.DoNothing);
        var sql = _mysql.Translate(query);
        Assert.Contains("ON DUPLICATE KEY UPDATE", sql);
    }

    [Fact]
    public void UpsertQuery_DoNothing_PostgreSqlUsesDoNothing()
    {
        var query = new UpsertQuery(
            "users",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "张三")],
            ["id"],
            UpsertStrategy.DoNothing);
        var sql = _pgsql.Translate(query);
        Assert.Contains("DO NOTHING", sql);
    }

    [Fact]
    public void UpsertQuery_DoNothing_SqliteUsesInsertOrIgnore()
    {
        var query = new UpsertQuery(
            "users",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "张三")],
            ["id"],
            UpsertStrategy.DoNothing);
        var sql = _sqlite.Translate(query);
        Assert.Contains("INSERT OR IGNORE INTO", sql);
    }

    #endregion

    #region 全文搜索语法差异

    [Fact]
    public void FullTextSearchQuery_PostgreSqlUsesTsVector()
    {
        var query = new FullTextSearchQuery("articles", ["title", "content"], "test", "simple");
        var sql = _pgsql.Translate(query);
        Assert.Contains("TO_TSVECTOR", sql);
        Assert.Contains("PLAINTO_TSQUERY", sql);
        Assert.Contains("@@", sql);
    }

    [Fact]
    public void FullTextSearchQuery_MySqlUsesMatchAgainst()
    {
        var query = new FullTextSearchQuery("articles", ["title", "content"], "test", "simple");
        var sql = _mysql.Translate(query);
        Assert.Contains("MATCH(", sql);
        Assert.Contains("AGAINST", sql);
    }

    [Fact]
    public void FullTextSearchQuery_SqliteUsesLike()
    {
        var query = new FullTextSearchQuery("articles", ["title", "content"], "test", "simple");
        var sql = _sqlite.Translate(query);
        Assert.Contains("LIKE '%test%'", sql);
        Assert.Contains("OR", sql);
    }

    #endregion

    #region CTE——三方言共享语法

    [Fact]
    public void CteQuery_AllDialectsSupportWith()
    {
        var innerQuery = new FindQuery("users", null, null, null);
        var cte = new CteQuery("orders",
            [new CteDefinition("active_users", innerQuery)],
            new FindQuery("active_users", null, null, null));

        var mysqlSql = _mysql.Translate(cte);
        var pgsqlSql = _pgsql.Translate(cte);
        var sqliteSql = _sqlite.Translate(cte);

        Assert.Contains("WITH ", mysqlSql);
        Assert.Contains("WITH ", pgsqlSql);
        Assert.Contains("WITH ", sqliteSql);
    }

    [Fact]
    public void CteQuery_Recursive_AllDialectsSupportWithRecursive()
    {
        var innerQuery = new FindQuery("categories", null, null, null);
        var cte = new CteQuery("categories",
            [new CteDefinition("tree", innerQuery, isRecursive: true)],
            new FindQuery("tree", null, null, null));

        var mysqlSql = _mysql.Translate(cte);
        var pgsqlSql = _pgsql.Translate(cte);
        var sqliteSql = _sqlite.Translate(cte);

        Assert.Contains("WITH RECURSIVE", mysqlSql);
        Assert.Contains("WITH RECURSIVE", pgsqlSql);
        Assert.Contains("WITH RECURSIVE", sqliteSql);
    }

    #endregion

    #region 字符串转义差异

    [Fact]
    public void FindQuery_StringEscape_MySqlUsesBackslash()
    {
        var query = new FindQuery("users", new FieldEquals("name", "O'Brien"), null, null);
        var sql = _mysql.Translate(query);
        Assert.Contains("\\'", sql);
    }

    [Fact]
    public void FindQuery_StringEscape_PostgreSqlUsesDoubleQuote()
    {
        var query = new FindQuery("users", new FieldEquals("name", "O'Brien"), null, null);
        var sql = _pgsql.Translate(query);
        Assert.Contains("''", sql);
    }

    [Fact]
    public void FindQuery_StringEscape_SqliteUsesDoubleQuote()
    {
        var query = new FindQuery("users", new FieldEquals("name", "O'Brien"), null, null);
        var sql = _sqlite.Translate(query);
        Assert.Contains("''", sql);
    }

    #endregion
}
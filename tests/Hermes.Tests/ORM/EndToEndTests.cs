using Hermes.Database.MySql;
using Hermes.Database.PostgreSql;
using Hermes.Database.Sqlite;
using Xunit;

namespace Hermes.ORM.Tests;

/// <summary>
///     端到端集成测试——验证从查询表达式构建到 SQL 翻译到内存执行的完整管线
/// </summary>
public sealed class EndToEndTests
{
    #region 完整 CRUD 生命周期

    [Fact]
    public async Task CrudLifecycle_InsertReadUpdateDelete_Success()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });

        var users = await executor.Query<User>().ToListAsync();
        Assert.Equal(2, users.Count);

        var user = await executor.Query<User>().Filter(new FieldEquals("Name", "张三")).FirstOrDefaultAsync();
        Assert.NotNull(user);
        Assert.Equal(25, user!.Age);

        var count = await executor.Query<User>().CountAsync();
        Assert.Equal(2, count);

        var found = await executor.Query<User>().Filter(new FieldEquals("Name", "张三")).FirstOrDefaultAsync();
        Assert.NotNull(found);

        await executor.Query<User>().Filter(new FieldEquals("Id", 1)).Set("Age", 26).UpdateAsync();
        var updated = await executor.Query<User>().Filter(new FieldEquals("Id", 1)).FirstOrDefaultAsync();
        Assert.Equal(26, updated!.Age);

        await executor.Query<User>().Filter(new FieldEquals("Id", 2)).DeleteAsync();
        var remaining = await executor.Query<User>().ToListAsync();
        Assert.Single(remaining);
    }

    #endregion

    #region CTE + 窗口函数组合

    [Fact]
    public void CteWithWindow_AllDialectsProduceValidSql()
    {
        var baseQuery = new FindQuery("orders", new FieldEquals("status", "active"), null, null);
        var cteQuery = new CteQuery("reports",
            [new CteDefinition("active_orders", baseQuery, ["id", "customer_id", "amount"])],
            new WindowQuery("active_orders",
            [
                new WindowFunctionDefinition(
                    WindowFunctionKind.Sum,
                    "amount",
                    "running_total",
                    partitionBy: ["customer_id"],
                    orderBy: [new WindowOrderItem("id", false)],
                    frame: new WindowFrame(
                        WindowFrameKind.Rows,
                        WindowFrameBound.UnboundedPreceding,
                        WindowFrameBound.CurrentRow))
            ]));

        var mysql = new MySqlQueryTranslator().Translate(cteQuery);
        var pgsql = new PostgreSqlQueryTranslator().Translate(cteQuery);
        var sqlite = new SqliteQueryTranslator().Translate(cteQuery);

        Assert.All([mysql, pgsql, sqlite], sql =>
        {
            Assert.Contains("WITH ", sql);
            Assert.Contains("active_orders", sql);
            Assert.Contains("SUM", sql);
            Assert.Contains("PARTITION BY", sql);
            Assert.Contains("ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW", sql);
        });
    }

    #endregion

    #region 全文搜索——三方言差异

    [Fact]
    public void FullTextSearch_AllDialectsUseDifferentSyntax()
    {
        var query = new FullTextSearchQuery(
            "articles",
            ["title", "body"],
            "database optimization",
            "english",
            FullTextSearchMode.Plain,
            orderByRank: true,
            pagination: new QueryPagination(0, 10));

        var mysql = new MySqlQueryTranslator().Translate(query);
        var pgsql = new PostgreSqlQueryTranslator().Translate(query);
        var sqlite = new SqliteQueryTranslator().Translate(query);

        Assert.Contains("MATCH(", mysql);
        Assert.Contains("AGAINST", mysql);

        Assert.Contains("TO_TSVECTOR", pgsql);
        Assert.Contains("PLAINTO_TSQUERY", pgsql);
        Assert.Contains("@@", pgsql);
        Assert.Contains("TS_RANK", pgsql);

        Assert.Contains("LIKE", sqlite);
        Assert.Contains("database optimization", sqlite);
    }

    #endregion

    #region 递归 CTE SQL 翻译

    [Fact]
    public void RecursiveCte_AllDialectsProduceWithRecursive()
    {
        var baseQuery = new FindQuery("categories", new FieldEquals("parent_id", null), null, null);
        var cte = new CteQuery("categories",
            [new CteDefinition("category_tree", baseQuery, isRecursive: true)],
            new FindQuery("category_tree", null, null, null));

        var mysql = new MySqlQueryTranslator().Translate(cte);
        var pgsql = new PostgreSqlQueryTranslator().Translate(cte);
        var sqlite = new SqliteQueryTranslator().Translate(cte);

        Assert.All([mysql, pgsql, sqlite], sql =>
        {
            Assert.Contains("WITH RECURSIVE", sql);
            Assert.Contains("category_tree", sql);
        });
    }

    #endregion

    #region 窗口函数——多种排名函数

    [Fact]
    public void MultipleWindowFunctions_AllDialectsProduceValidSql()
    {
        var query = new WindowQuery("scores",
        [
            new WindowFunctionDefinition(WindowFunctionKind.RowNumber, "score", "rn",
                orderBy: [new WindowOrderItem("score", true)]),
            new WindowFunctionDefinition(WindowFunctionKind.Rank, "score", "rank",
                orderBy: [new WindowOrderItem("score", true)]),
            new WindowFunctionDefinition(WindowFunctionKind.DenseRank, "score", "dense_rank",
                orderBy: [new WindowOrderItem("score", true)])
        ]);

        var mysql = new MySqlQueryTranslator().Translate(query);
        var pgsql = new PostgreSqlQueryTranslator().Translate(query);
        var sqlite = new SqliteQueryTranslator().Translate(query);

        Assert.All([mysql, pgsql, sqlite], sql =>
        {
            Assert.Contains("ROW_NUMBER()", sql);
            Assert.Contains("RANK()", sql);
            Assert.Contains("DENSE_RANK()", sql);
        });
    }

    #endregion

    #region 多方言 SQL 翻译一致性

    [Fact]
    public void SameQuery_AllDialectsProduceValidSql()
    {
        var query = new FindQuery(
            "products",
            new AndPredicate(
                new FieldGreaterThan("price", 100),
                new FieldContains("name", "Widget")),
            new QueryOrdering("price", true),
            new QueryPagination(0, 20));

        var mysql = new MySqlQueryTranslator().Translate(query);
        var pgsql = new PostgreSqlQueryTranslator().Translate(query);
        var sqlite = new SqliteQueryTranslator().Translate(query);

        Assert.All([mysql, pgsql, sqlite], sql =>
        {
            Assert.Contains("SELECT", sql);
            Assert.Contains("FROM", sql);
            Assert.Contains("WHERE", sql);
            Assert.Contains("ORDER BY", sql);
            Assert.Contains("LIMIT", sql);
        });
    }

    [Fact]
    public void UpsertQuery_AllDialectsProduceValidSql()
    {
        var query = new UpsertQuery(
            "products",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "Widget"), new FieldAssignment("price", 99.9)],
            ["id"],
            UpsertStrategy.DoUpdate);

        var mysql = new MySqlQueryTranslator().Translate(query);
        var pgsql = new PostgreSqlQueryTranslator().Translate(query);
        var sqlite = new SqliteQueryTranslator().Translate(query);

        Assert.All([mysql, pgsql, sqlite], sql =>
        {
            Assert.Contains("INSERT", sql);
            Assert.Contains("99.9", sql);
        });

        Assert.Contains("ON DUPLICATE KEY UPDATE", mysql);
        Assert.Contains("ON CONFLICT", pgsql);
        Assert.Contains("INSERT OR REPLACE", sqlite);
    }

    #endregion

    #region DefaultQueryExecutor 内存执行

    [Fact]
    public async Task DefaultQueryExecutor_UpsertInMemory_Success()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });

        var upsertQuery = new UpsertQuery(
            "User",
            [new FieldAssignment("Id", 1), new FieldAssignment("Name", "张三丰"), new FieldAssignment("Age", 26)],
            ["Id"],
            UpsertStrategy.DoUpdate);

        var result = await executor.ExecuteRawAsync(upsertQuery);
        Assert.True(result.Success, $"Upsert 失败：{result.Error}");

        var user = await executor.Query<User>().Filter(new FieldEquals("Id", 1)).FirstOrDefaultAsync();
        Assert.NotNull(user);
    }

    [Fact]
    public async Task DefaultQueryExecutor_AggregateInMemory_Success()
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        await executor.Query<User>().InsertAsync(new User { Id = 1, Name = "张三", Age = 25 });
        await executor.Query<User>().InsertAsync(new User { Id = 2, Name = "李四", Age = 30 });
        await executor.Query<User>().InsertAsync(new User { Id = 3, Name = "王五", Age = 35 });

        var count = await executor.Query<User>().CountAsync();
        Assert.Equal(3, count);
    }

    #endregion
}
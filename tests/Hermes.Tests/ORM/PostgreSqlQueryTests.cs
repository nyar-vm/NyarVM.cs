using Hermes.Database.PostgreSql;
using Xunit;

namespace Hermes.ORM.Tests;

/// <summary>
///     PostgreSQL 高级查询类型测试
/// </summary>
public sealed class PostgreSqlQueryTests
{
    [Fact]
    public void CteQuery_NonRecursive_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var innerQuery = new FindQuery("users", null, new QueryOrdering("name", false), null);
        var cte = new CteQuery("orders",
            [new CteDefinition("active_users", innerQuery, ["id", "name"])],
            new FindQuery("active_users", null, null, null));

        var sql = translator.Translate(cte);

        Assert.Contains("WITH", sql);
        Assert.Contains("active_users", sql);
        Assert.Contains("SELECT", sql);
    }

    [Fact]
    public void CteQuery_Recursive_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var baseQuery = new FindQuery("categories", new FieldEquals("parent_id", null), null, null);
        var recursiveQuery = new FindQuery("categories", null, null, null);
        var cte = new CteQuery("categories",
            [new CteDefinition("category_tree", baseQuery, isRecursive: true)],
            new FindQuery("category_tree", null, null, null));

        var sql = translator.Translate(cte);

        Assert.Contains("WITH RECURSIVE", sql);
        Assert.Contains("category_tree", sql);
    }

    [Fact]
    public void WindowQuery_RowNumber_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var windowQuery = new WindowQuery("employees",
        [
            new WindowFunctionDefinition(
                WindowFunctionKind.RowNumber,
                "salary",
                "row_num",
                partitionBy: ["department"],
                orderBy: [new WindowOrderItem("salary", true)])
        ]);

        var sql = translator.Translate(windowQuery);

        Assert.Contains("ROW_NUMBER()", sql);
        Assert.Contains("PARTITION BY", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("AS \"row_num\"", sql);
    }

    [Fact]
    public void WindowQuery_Rank_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var windowQuery = new WindowQuery("scores",
        [
            new WindowFunctionDefinition(
                WindowFunctionKind.Rank,
                "score",
                "rank",
                orderBy: [new WindowOrderItem("score", true)])
        ]);

        var sql = translator.Translate(windowQuery);

        Assert.Contains("RANK()", sql);
        Assert.Contains("AS \"rank\"", sql);
    }

    [Fact]
    public void WindowQuery_Lag_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var windowQuery = new WindowQuery("metrics",
        [
            new WindowFunctionDefinition(
                WindowFunctionKind.Lag,
                "value",
                "prev_value",
                orderBy: [new WindowOrderItem("timestamp", false)],
                offset: 1)
        ]);

        var sql = translator.Translate(windowQuery);

        Assert.Contains("LAG(\"value\", 1)", sql);
        Assert.Contains("AS \"prev_value\"", sql);
    }

    [Fact]
    public void WindowQuery_LeadWithDefault_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var windowQuery = new WindowQuery("metrics",
        [
            new WindowFunctionDefinition(
                WindowFunctionKind.Lead,
                "value",
                "next_value",
                orderBy: [new WindowOrderItem("timestamp", false)],
                offset: 1,
                defaultValue: 0)
        ]);

        var sql = translator.Translate(windowQuery);

        Assert.Contains("LEAD(\"value\", 1, 0)", sql);
    }

    [Fact]
    public void WindowQuery_WithFrame_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var windowQuery = new WindowQuery("sales",
        [
            new WindowFunctionDefinition(
                WindowFunctionKind.Sum,
                "amount",
                "running_total",
                orderBy: [new WindowOrderItem("date", false)],
                frame: new WindowFrame(
                    WindowFrameKind.Rows,
                    WindowFrameBound.UnboundedPreceding,
                    WindowFrameBound.CurrentRow))
        ]);

        var sql = translator.Translate(windowQuery);

        Assert.Contains("SUM(\"amount\")", sql);
        Assert.Contains("ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW", sql);
    }

    [Fact]
    public void FullTextSearchQuery_PlainMode_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var ftsQuery = new FullTextSearchQuery(
            "articles",
            ["title", "content"],
            "数据库优化",
            "simple",
            FullTextSearchMode.Plain,
            orderByRank: true);

        var sql = translator.Translate(ftsQuery);

        Assert.Contains("TO_TSVECTOR", sql);
        Assert.Contains("PLAINTO_TSQUERY", sql);
        Assert.Contains("@@", sql);
        Assert.Contains("TS_RANK", sql);
        Assert.Contains("simple", sql);
    }

    [Fact]
    public void FullTextSearchQuery_WebSearchMode_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var ftsQuery = new FullTextSearchQuery(
            "documents",
            ["body"],
            "postgresql OR mysql",
            "english",
            FullTextSearchMode.WebSearch);

        var sql = translator.Translate(ftsQuery);

        Assert.Contains("WEBSEARCH_TO_TSQUERY", sql);
        Assert.Contains("english", sql);
    }

    [Fact]
    public void FullTextSearchQuery_MultipleFields_CombinesTsvector()
    {
        var translator = new PostgreSqlQueryTranslator();

        var ftsQuery = new FullTextSearchQuery(
            "posts",
            ["title", "body", "tags"],
            "test query",
            "simple",
            FullTextSearchMode.Plain);

        var sql = translator.Translate(ftsQuery);

        Assert.Contains("||", sql);
        Assert.Contains("TO_TSVECTOR", sql);
        Assert.Contains("COALESCE", sql);
    }

    [Fact]
    public void UpsertQuery_DoUpdate_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var upsertQuery = new UpsertQuery(
            "users",
            [
                new FieldAssignment("id", 1), new FieldAssignment("name", "张三"),
                new FieldAssignment("email", "z@test.com")
            ],
            ["id"],
            UpsertStrategy.DoUpdate);

        var sql = translator.Translate(upsertQuery);

        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("ON CONFLICT (\"id\")", sql);
        Assert.Contains("DO UPDATE SET", sql);
        Assert.Contains("EXCLUDED", sql);
        Assert.Contains("RETURNING *", sql);
    }

    [Fact]
    public void UpsertQuery_DoNothing_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var upsertQuery = new UpsertQuery(
            "users",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "李四")],
            ["id"],
            UpsertStrategy.DoNothing);

        var sql = translator.Translate(upsertQuery);

        Assert.Contains("ON CONFLICT (\"id\")", sql);
        Assert.Contains("DO NOTHING", sql);
    }

    [Fact]
    public void UpsertQuery_WithCustomUpdate_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var upsertQuery = new UpsertQuery(
            "products",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "Widget"), new FieldAssignment("price", 99.9)],
            ["id"],
            UpsertStrategy.DoUpdate,
            [new FieldAssignment("price", 99.9), new FieldAssignment("name", "Widget")]);

        var sql = translator.Translate(upsertQuery);

        Assert.Contains("DO UPDATE SET", sql);
        Assert.Contains("\"price\"", sql);
        Assert.Contains("\"name\"", sql);
    }

    [Fact]
    public void WindowQuery_DenseRankWithPagination_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var windowQuery = new WindowQuery("students",
            [
                new WindowFunctionDefinition(
                    WindowFunctionKind.DenseRank,
                    "score",
                    "dense_rank",
                    orderBy: [new WindowOrderItem("score", true)])
            ],
            pagination: new QueryPagination(0, 10));

        var sql = translator.Translate(windowQuery);

        Assert.Contains("DENSE_RANK()", sql);
        Assert.Contains("LIMIT 10", sql);
    }

    [Fact]
    public void FullTextSearchQuery_WithFilter_TranslatesCorrectly()
    {
        var translator = new PostgreSqlQueryTranslator();

        var ftsQuery = new FullTextSearchQuery(
            "articles",
            ["title"],
            "test",
            "simple",
            FullTextSearchMode.Plain,
            predicate: new FieldEquals("published", true),
            orderByRank: true);

        var sql = translator.Translate(ftsQuery);

        Assert.Contains("@@", sql);
        Assert.Contains("AND", sql);
        Assert.Contains("\"published\" = TRUE", sql);
    }
}
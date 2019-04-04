using System.Diagnostics;
using Hermes.Database.MySql;
using Hermes.Database.PostgreSql;
using Hermes.Database.Sqlite;
using Xunit;
using Xunit.Abstractions;

namespace Hermes.ORM.Tests;

/// <summary>
///     代码生成与查询翻译性能基准测试
/// </summary>
public sealed class PerformanceBenchmarkTests(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    #region 内存执行性能基准

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task CrudPerformance_MemoryExecutor(int entityCount)
    {
        var db = new MockDatabase();
        var executor = new DefaultQueryExecutor(db.CreateExecutor());

        var sw = Stopwatch.StartNew();
        for (var i = 0; i < entityCount; i++)
            await executor.Query<User>().InsertAsync(new User { Id = i, Name = $"User{i}", Age = 20 + i % 50 });

        var insertMs = sw.ElapsedMilliseconds;

        sw.Restart();
        var all = await executor.Query<User>().ToListAsync();
        var findAllMs = sw.ElapsedMilliseconds;

        sw.Restart();
        var count = await executor.Query<User>().CountAsync();
        var countMs = sw.ElapsedMilliseconds;

        sw.Restart();
        for (var i = 0; i < entityCount; i += 10)
            await executor.Query<User>().Filter(new FieldEquals("Id", i)).Set("Age", 30).UpdateAsync();

        var updateMs = sw.ElapsedMilliseconds;

        sw.Restart();
        for (var i = 0; i < entityCount; i += 20)
            await executor.Query<User>().Filter(new FieldEquals("Id", i)).DeleteAsync();

        var deleteMs = sw.ElapsedMilliseconds;

        _output.WriteLine($"CRUD 性能 ({entityCount} 实体):");
        _output.WriteLine($"  Insert {entityCount}: {insertMs} ms");
        _output.WriteLine($"  FindAll: {findAllMs} ms ({all.Count} 条)");
        _output.WriteLine($"  Count: {countMs} ms (结果: {count})");
        _output.WriteLine($"  Update {entityCount / 10}: {updateMs} ms");
        _output.WriteLine($"  Delete {entityCount / 20}: {deleteMs} ms");

        Assert.Equal(entityCount, count);
        Assert.Equal(entityCount - entityCount / 20, (await executor.Query<User>().ToListAsync()).Count);
    }

    #endregion

    #region 辅助方法

    private static double MeasureMs(Action action)
    {
        var sw = Stopwatch.StartNew();
        action();
        sw.Stop();
        return sw.Elapsed.TotalMilliseconds;
    }

    #endregion

    #region 查询翻译性能基准

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void FindQuery_TranslationPerformance_AllDialects(int iterations)
    {
        var query = new FindQuery(
            "users",
            new AndPredicate(
                new FieldGreaterThan("age", 18),
                new FieldContains("name", "test")),
            new QueryOrdering("name", false),
            new QueryPagination(0, 50));

        var mysql = new MySqlQueryTranslator();
        var pgsql = new PostgreSqlQueryTranslator();
        var sqlite = new SqliteQueryTranslator();

        var mysqlMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) mysql.Translate(query);
        });
        var pgsqlMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) pgsql.Translate(query);
        });
        var sqliteMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) sqlite.Translate(query);
        });

        _output.WriteLine($"FindQuery 翻译性能 ({iterations} 次迭代):");
        _output.WriteLine($"  MySQL:     {mysqlMs:F2} ms ({mysqlMs / iterations * 1000:F0} μs/次)");
        _output.WriteLine($"  PostgreSQL: {pgsqlMs:F2} ms ({pgsqlMs / iterations * 1000:F0} μs/次)");
        _output.WriteLine($"  SQLite:    {sqliteMs:F2} ms ({sqliteMs / iterations * 1000:F0} μs/次)");

        Assert.True(mysqlMs < 5000, $"MySQL 翻译 {iterations} 次耗时 {mysqlMs}ms，超过 5s 阈值");
        Assert.True(pgsqlMs < 5000, $"PostgreSQL 翻译 {iterations} 次耗时 {pgsqlMs}ms，超过 5s 阈值");
        Assert.True(sqliteMs < 5000, $"SQLite 翻译 {iterations} 次耗时 {sqliteMs}ms，超过 5s 阈值");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void UpsertQuery_TranslationPerformance_AllDialects(int iterations)
    {
        var query = new UpsertQuery(
            "users",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "张三"), new FieldAssignment("age", 25)],
            ["id"],
            UpsertStrategy.DoUpdate);

        var mysql = new MySqlQueryTranslator();
        var pgsql = new PostgreSqlQueryTranslator();
        var sqlite = new SqliteQueryTranslator();

        var mysqlMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) mysql.Translate(query);
        });
        var pgsqlMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) pgsql.Translate(query);
        });
        var sqliteMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) sqlite.Translate(query);
        });

        _output.WriteLine($"UpsertQuery 翻译性能 ({iterations} 次迭代):");
        _output.WriteLine($"  MySQL:     {mysqlMs:F2} ms ({mysqlMs / iterations * 1000:F0} μs/次)");
        _output.WriteLine($"  PostgreSQL: {pgsqlMs:F2} ms ({pgsqlMs / iterations * 1000:F0} μs/次)");
        _output.WriteLine($"  SQLite:    {sqliteMs:F2} ms ({sqliteMs / iterations * 1000:F0} μs/次)");

        Assert.True(mysqlMs < 3000);
        Assert.True(pgsqlMs < 3000);
        Assert.True(sqliteMs < 3000);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void WindowQuery_TranslationPerformance_AllDialects(int iterations)
    {
        var query = new WindowQuery("employees",
        [
            new WindowFunctionDefinition(WindowFunctionKind.RowNumber, "salary", "rn",
                partitionBy: ["department"],
                orderBy: [new WindowOrderItem("salary", true)]),
            new WindowFunctionDefinition(WindowFunctionKind.Rank, "salary", "rank",
                partitionBy: ["department"],
                orderBy: [new WindowOrderItem("salary", true)]),
            new WindowFunctionDefinition(WindowFunctionKind.Sum, "salary", "running_total",
                partitionBy: ["department"],
                orderBy: [new WindowOrderItem("id", false)],
                frame: new WindowFrame(WindowFrameKind.Rows,
                    WindowFrameBound.UnboundedPreceding,
                    WindowFrameBound.CurrentRow))
        ]);

        var mysql = new MySqlQueryTranslator();
        var pgsql = new PostgreSqlQueryTranslator();
        var sqlite = new SqliteQueryTranslator();

        var mysqlMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) mysql.Translate(query);
        });
        var pgsqlMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) pgsql.Translate(query);
        });
        var sqliteMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) sqlite.Translate(query);
        });

        _output.WriteLine($"WindowQuery 翻译性能 ({iterations} 次迭代):");
        _output.WriteLine($"  MySQL:     {mysqlMs:F2} ms ({mysqlMs / iterations * 1000:F0} μs/次)");
        _output.WriteLine($"  PostgreSQL: {pgsqlMs:F2} ms ({pgsqlMs / iterations * 1000:F0} μs/次)");
        _output.WriteLine($"  SQLite:    {sqliteMs:F2} ms ({sqliteMs / iterations * 1000:F0} μs/次)");

        Assert.True(mysqlMs < 3000);
        Assert.True(pgsqlMs < 3000);
        Assert.True(sqliteMs < 3000);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void FullTextSearchQuery_TranslationPerformance_AllDialects(int iterations)
    {
        var query = new FullTextSearchQuery("articles", ["title", "content", "tags"],
            "database optimization performance", "english",
            FullTextSearchMode.WebSearch,
            predicate: new FieldEquals("published", true),
            orderByRank: true,
            pagination: new QueryPagination(0, 20));

        var mysql = new MySqlQueryTranslator();
        var pgsql = new PostgreSqlQueryTranslator();
        var sqlite = new SqliteQueryTranslator();

        var mysqlMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) mysql.Translate(query);
        });
        var pgsqlMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) pgsql.Translate(query);
        });
        var sqliteMs = MeasureMs(() =>
        {
            for (var i = 0; i < iterations; i++) sqlite.Translate(query);
        });

        _output.WriteLine($"FullTextSearchQuery 翻译性能 ({iterations} 次迭代):");
        _output.WriteLine($"  MySQL:     {mysqlMs:F2} ms ({mysqlMs / iterations * 1000:F0} μs/次)");
        _output.WriteLine($"  PostgreSQL: {pgsqlMs:F2} ms ({pgsqlMs / iterations * 1000:F0} μs/次)");
        _output.WriteLine($"  SQLite:    {sqliteMs:F2} ms ({sqliteMs / iterations * 1000:F0} μs/次)");

        Assert.True(mysqlMs < 3000);
        Assert.True(pgsqlMs < 3000);
        Assert.True(sqliteMs < 3000);
    }

    #endregion
}
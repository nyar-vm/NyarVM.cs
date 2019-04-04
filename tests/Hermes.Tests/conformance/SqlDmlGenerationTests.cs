using Hermes.Compiler;
using Xunit;

namespace Hermes.Tests.Conformance;

public class SqlDmlGenerationTests
{
    #region DELETE 生成

    [Fact]
    public void GenerateDelete_UsesPrimaryKeyInWhere()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
}");
        var dml = CreateDml("mysql");
        var sql = dml.GenerateDelete(model);

        Assert.Contains("DELETE FROM", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("@id", sql);
    }

    #endregion

    #region 蛇形命名

    [Fact]
    public void GenerateDml_SnakeCaseTableName()
    {
        var (model, schema) = CompileModel(@"
model UserProfile {
    @@id: i64,
    display_name: utf8,
}");
        var dml = CreateDml("mysql");
        var sql = dml.GenerateSelectAll(model, schema);

        Assert.Contains("user_profile", sql);
        Assert.DoesNotContain("UserProfile", sql);
    }

    #endregion

    #region INSERT 生成

    [Fact]
    public void GenerateInsert_MySql_ProducesInsertWithColumns()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
    title: utf8,
    content: utf8,
}");
        var dml = CreateDml("mysql");
        var sql = dml.GenerateInsert(model, schema);

        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("`post`", sql);
        Assert.Contains("VALUES", sql);
        Assert.DoesNotContain("RETURNING", sql);
    }

    [Fact]
    public void GenerateInsert_PostgreSql_IncludesReturning()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
    title: utf8,
}");
        var dml = CreateDml("postgresql");
        var sql = dml.GenerateInsert(model, schema);

        Assert.Contains("INSERT INTO", sql);
        Assert.Contains("RETURNING", sql);
    }

    [Fact]
    public void GenerateInsert_IncludesAllFields()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
    title: utf8,
}");
        var dml = CreateDml("mysql");
        var sql = dml.GenerateInsert(model, schema);

        Assert.Contains("@id", sql);
        Assert.Contains("@title", sql);
    }

    #endregion

    #region SELECT 生成

    [Fact]
    public void GenerateSelectById_UsesPrimaryKeyInWhere()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
    title: utf8,
}");
        var dml = CreateDml("mysql");
        var sql = dml.GenerateSelectById(model, schema);

        Assert.Contains("SELECT", sql);
        Assert.Contains("FROM", sql);
        Assert.Contains("WHERE `id` = @id", sql);
    }

    [Fact]
    public void GenerateSelectAll_ProducesFullTableScan()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
    title: utf8,
}");
        var dml = CreateDml("mysql");
        var sql = dml.GenerateSelectAll(model, schema);

        Assert.Contains("SELECT", sql);
        Assert.Contains("FROM", sql);
        Assert.DoesNotContain("WHERE", sql);
    }

    #endregion

    #region UPDATE 生成

    [Fact]
    public void GenerateUpdate_SetsNonKeyColumns()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
    title: utf8,
    content: utf8,
}");
        var dml = CreateDml("mysql");
        var sql = dml.GenerateUpdate(model, schema);

        Assert.Contains("UPDATE", sql);
        Assert.Contains("SET", sql);
        Assert.Contains("@title", sql);
        Assert.Contains("@content", sql);

        var lines = sql.Split('\n');
        var setLines = lines.Where(l => l.Contains("SET") || l.Contains("@"));
        Assert.Contains(setLines, l => l.Contains("@title") && !l.Contains("@id"));
    }

    [Fact]
    public void GenerateUpdate_OnlyPkModel_ReturnsComment()
    {
        var (model, schema) = CompileModel(@"
model Config {
    @@key: utf8,
}");
        var dml = CreateDml("mysql");
        var sql = dml.GenerateUpdate(model, schema);

        Assert.Contains("no updatable columns", sql, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region UPSERT 生成

    [Fact]
    public void GenerateUpsert_MySql_UsesOnDuplicateKey()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
    title: utf8,
}");
        var dml = CreateDml("mysql");
        var sql = dml.GenerateUpsert(model, schema);

        Assert.Contains("ON DUPLICATE KEY UPDATE", sql);
    }

    [Fact]
    public void GenerateUpsert_PostgreSql_UsesOnConflict()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
    title: utf8,
}");
        var dml = CreateDml("postgresql");
        var sql = dml.GenerateUpsert(model, schema);

        Assert.Contains("ON CONFLICT", sql);
    }

    [Fact]
    public void GenerateUpsert_Sqlite_UsesOnConflictDoUpdate()
    {
        var (model, schema) = CompileModel(@"
model Post {
    @@id: i64,
    title: utf8,
}");
        var dml = CreateDml("sqlite");
        var sql = dml.GenerateUpsert(model, schema);

        Assert.Contains("ON CONFLICT DO UPDATE SET", sql);
    }

    #endregion

    #region 辅助方法

    private static (ModelDefinition model, SchemaIR schema) CompileModel(string source)
    {
        var compiler = new HermesCompiler();
        var result = compiler.CompileSource(source);

        if (!result.Success)
        {
            var messages = string.Join("; ", result.Diagnostics.Diagnostics.Select(d => d.Message));
            throw new InvalidOperationException($"编译失败: {messages}");
        }

        var storage = result.Schema!.Storages[0];
        var model = storage.Models[0];
        return (model, result.Schema);
    }

    private static DmlGenerator CreateDml(string dialectName)
    {
        var dialect = SqlDialect.FromName(dialectName);
        return new DmlGenerator(dialect);
    }

    #endregion
}
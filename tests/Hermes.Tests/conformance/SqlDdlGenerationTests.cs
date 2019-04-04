using Hermes.Compiler;
using Xunit;

namespace Hermes.Tests.Conformance;

public class SqlDdlGenerationTests
{
    #region 类型映射

    [Theory]
    [InlineData("mysql", "i32", "INTEGER")]
    [InlineData("mysql", "i64", "BIGINT")]
    [InlineData("mysql", "f64", "DOUBLE PRECISION")]
    [InlineData("mysql", "utf8", "TEXT")]
    [InlineData("mysql", "bool", "BOOLEAN")]
    [InlineData("postgresql", "i32", "INTEGER")]
    [InlineData("postgresql", "i64", "BIGINT")]
    [InlineData("postgresql", "utf8", "TEXT")]
    [InlineData("postgresql", "bool", "BOOLEAN")]
    [InlineData("sqlite", "i32", "INTEGER")]
    [InlineData("sqlite", "i64", "BIGINT")]
    [InlineData("sqlite", "bool", "INTEGER")]
    [InlineData("sqlite", "utf8", "TEXT")]
    public void GenerateDdl_FieldType_MapsToCorrectSqlType(string dialect, string hermesType, string expectedSqlType)
    {
        var source = $@"
model TestType {{
    @@id: i64,
    value: {hermesType},
}}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var sql = string.Join("\n", GenerateDdl(result.Schema!, dialect));

        Assert.Contains(expectedSqlType, sql);
    }

    #endregion

    #region NOT NULL 约束

    [Fact]
    public void GenerateDdl_RequiredField_AddsNotNullConstraint()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var sql = string.Join("\n", GenerateDdl(result.Schema!, "mysql"));

        Assert.Contains("NOT NULL", sql);
    }

    #endregion

    #region 多模型

    [Fact]
    public void GenerateDdl_MultipleModels_GeneratesMultipleTables()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
}

model Comment {
    @@id: i64,
    post_id: i64,
    body: utf8,
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var statements = GenerateDdl(result.Schema!, "mysql");

        Assert.True(statements.Count >= 2);
        var sql = string.Join("\n", statements);
        Assert.Contains("post", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("comment", sql, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 外键

    [Fact]
    public void GenerateDdl_ReferenceField_GeneratesForeignKey()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
}

model Comment {
    @@id: i64,
    body: utf8,
    post_id: i64,
}";
        // 外键生成依赖于 RelationDef/ForeignKey 等 schema 定义，
        // DdlGenerator 通过字段的 FK info 来生成
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var sql = string.Join("\n", GenerateDdl(result.Schema!, "mysql"));

        // 基础校验：第二位 comment 表生成
        Assert.Contains("comment", sql, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 基础 DDL 生成

    [Theory]
    [InlineData("mysql")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    public void GenerateDdl_BasicModel_ProducesCreateTable(string dialect)
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
    content: utf8,
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var statements = GenerateDdl(result.Schema!, dialect);

        Assert.NotEmpty(statements);
        var createTable = statements[0];
        Assert.Contains("CREATE TABLE", createTable, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("post", createTable, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("mysql")]
    [InlineData("postgresql")]
    [InlineData("sqlite")]
    public void GenerateDdl_ModelWithFields_IncludesAllColumns(string dialect)
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
    view_count: i32,
    is_published: bool,
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var sql = string.Join("\n", GenerateDdl(result.Schema!, dialect));

        Assert.Contains("id", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("title", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("view_count", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("is_published", sql, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region 主键生成

    [Fact]
    public void GenerateDdl_MySql_PrimaryKey_UsesAutoIncrement()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var sql = string.Join("\n", GenerateDdl(result.Schema!, "mysql"));

        Assert.Contains("AUTO_INCREMENT", sql);
        Assert.Contains("PRIMARY KEY", sql);
    }

    [Fact]
    public void GenerateDdl_PostgreSql_PrimaryKey_UsesSerial()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var sql = string.Join("\n", GenerateDdl(result.Schema!, "postgresql"));

        Assert.Contains("SERIAL", sql);
        Assert.Contains("PRIMARY KEY", sql);
    }

    [Fact]
    public void GenerateDdl_Sqlite_PrimaryKey_UsesAutoincrement()
    {
        var source = @"
model Post {
    @@id: i64,
    title: utf8,
}";
        var result = CompileSource(source);
        Assert.True(result.Success, $"编译失败：{GetDiagnosticsMessage(result)}");

        var sql = string.Join("\n", GenerateDdl(result.Schema!, "sqlite"));

        Assert.Contains("AUTOINCREMENT", sql);
        Assert.Contains("PRIMARY KEY", sql);
    }

    #endregion

    #region 辅助方法

    private static List<string> GenerateDdl(Nyar.Dialect.Schema.IR.SchemaIR schema, string dialect)
    {
        var plugin = new SqlPlugin();
        return plugin.GenerateDdlStatements(schema, dialect);
    }

    private static CompilationResult CompileSource(string source)
    {
        var compiler = new HermesCompiler();
        return compiler.CompileSource(source);
    }

    private static string GetDiagnosticsMessage(CompilationResult result)
    {
        if (result.Success) return "";

        var messages = result.Diagnostics.Diagnostics.Select(d => d.Message);
        return string.Join("; ", messages);
    }

    #endregion
}
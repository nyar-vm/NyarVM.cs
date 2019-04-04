using Hermes.Database.Schema;
using Hermes.Plugin.Sql;
using Hermes.Plugin.Sql.Ddl;
using Hermes.YYDB.Query;

namespace Hermes.Database.MySql.Tests;

public sealed class MySqlMigrationTests
{
    #region 批量插入测试

    [Fact]
    public void TranslateBatchInsert_单行_生成正确SQL()
    {
        var translator = new MySqlQueryTranslator();
        var query = new BatchInsertQuery("users",
        [
            [new FieldAssignment("name", "Alice"), new FieldAssignment("age", 30)]
        ]);

        var sql = translator.Translate(query);

        Assert.Contains("INSERT INTO `users`", sql);
        Assert.Contains("`name`, `age`", sql);
        Assert.Contains("'Alice', 30", sql);
    }

    [Fact]
    public void TranslateBatchInsert_多行_生成VALUES列表()
    {
        var translator = new MySqlQueryTranslator();
        var query = new BatchInsertQuery("users",
        [
            [new FieldAssignment("name", "Alice"), new FieldAssignment("age", 30)],
            [new FieldAssignment("name", "Bob"), new FieldAssignment("age", 25)],
            [new FieldAssignment("name", "Charlie"), new FieldAssignment("age", 35)]
        ]);

        var sql = translator.Translate(query);

        Assert.Contains("INSERT INTO `users`", sql);
        Assert.Contains("('Alice', 30)", sql);
        Assert.Contains("('Bob', 25)", sql);
        Assert.Contains("('Charlie', 35)", sql);
    }

    [Fact]
    public void TranslateBatchInsert_批次大小_分割为多条INSERT()
    {
        var translator = new MySqlQueryTranslator();
        var rows = new List<IReadOnlyList<FieldAssignment>>();

        for (var i = 0; i < 5; i++)
            rows.Add([new FieldAssignment("name", $"User{i}"), new FieldAssignment("age", 20 + i)]);

        var query = new BatchInsertQuery("users", rows, 2);
        var sql = translator.Translate(query);

        var insertCount = sql.Split("INSERT INTO", StringSplitOptions.RemoveEmptyEntries).Length;
        Assert.Equal(3, insertCount);
    }

    [Fact]
    public void TranslateBatchInsert_空行_不生成SQL()
    {
        var translator = new MySqlQueryTranslator();
        var query = new BatchInsertQuery("users", []);

        var sql = translator.Translate(query);

        Assert.Empty(sql);
    }

    [Fact]
    public void TranslateBatchInsert_NULL值_正确处理()
    {
        var translator = new MySqlQueryTranslator();
        var query = new BatchInsertQuery("users",
        [
            [new FieldAssignment("name", "Alice"), new FieldAssignment("bio", null!)]
        ]);

        var sql = translator.Translate(query);

        Assert.Contains("NULL", sql);
    }

    #endregion

    #region MySQL DDL Migration 测试

    [Fact]
    public void GenerateMigrationSql_MySQL_添加列_使用ADD_COLUMN()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableModified,
                    TableName = "users",
                    ColumnDiffs =
                    [
                        new ColumnDiff
                        {
                            Type = SchemaDiffType.ColumnAdded,
                            ColumnName = "email",
                            NewType = "VARCHAR(255)"
                        }
                    ]
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("ALTER TABLE", statements[0]);
        Assert.Contains("ADD COLUMN", statements[0]);
        Assert.Contains("email", statements[0]);
    }

    [Fact]
    public void GenerateMigrationSql_MySQL_修改列_使用MODIFY_COLUMN()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableModified,
                    TableName = "users",
                    ColumnDiffs =
                    [
                        new ColumnDiff
                        {
                            Type = SchemaDiffType.ColumnModified,
                            ColumnName = "age",
                            OldType = "TINYINT",
                            NewType = "INT"
                        }
                    ]
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("MODIFY COLUMN", statements[0]);
        Assert.Contains("age", statements[0]);
    }

    [Fact]
    public void GenerateMigrationSql_MySQL_删除列_生成DROP_COLUMN()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableModified,
                    TableName = "users",
                    ColumnDiffs =
                    [
                        new ColumnDiff
                        {
                            Type = SchemaDiffType.ColumnRemoved,
                            ColumnName = "legacy_field"
                        }
                    ]
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("DROP COLUMN", statements[0]);
        Assert.Contains("legacy_field", statements[0]);
    }

    [Fact]
    public void GenerateMigrationSql_MySQL_删除表_生成DROP_TABLE()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableRemoved,
                    TableName = "deprecated_table"
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("DROP TABLE IF EXISTS", statements[0]);
        Assert.Contains("deprecated_table", statements[0]);
    }

    [Fact]
    public void GenerateMigrationSql_MySQL_添加表_生成Note()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableAdded,
                    TableName = "orders"
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("orders", statements[0]);
    }

    [Fact]
    public void GenerateMigrationSql_MySQL_多列变更_生成多条ALTER()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableModified,
                    TableName = "products",
                    ColumnDiffs =
                    [
                        new ColumnDiff
                        {
                            Type = SchemaDiffType.ColumnAdded,
                            ColumnName = "description",
                            NewType = "TEXT"
                        },
                        new ColumnDiff
                        {
                            Type = SchemaDiffType.ColumnModified,
                            ColumnName = "price",
                            OldType = "DECIMAL(8,2)",
                            NewType = "DECIMAL(10,4)"
                        },
                        new ColumnDiff
                        {
                            Type = SchemaDiffType.ColumnRemoved,
                            ColumnName = "old_price"
                        }
                    ]
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Equal(3, statements.Count);
        Assert.Contains(statements, s => s.Contains("ADD COLUMN") && s.Contains("description"));
        Assert.Contains(statements, s => s.Contains("MODIFY COLUMN") && s.Contains("price"));
        Assert.Contains(statements, s => s.Contains("DROP COLUMN") && s.Contains("old_price"));
    }

    [Fact]
    public void GenerateMigrationSql_MySQL_VARCHAR长度变更_使用MODIFY()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableModified,
                    TableName = "users",
                    ColumnDiffs =
                    [
                        new ColumnDiff
                        {
                            Type = SchemaDiffType.ColumnModified,
                            ColumnName = "username",
                            OldType = "VARCHAR(50)",
                            NewType = "VARCHAR(100)"
                        }
                    ]
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("MODIFY COLUMN", statements[0]);
        Assert.Contains("username", statements[0]);
    }

    [Fact]
    public void GenerateMigrationSql_MySQL_DATETIME精度变更_使用MODIFY()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableModified,
                    TableName = "events",
                    ColumnDiffs =
                    [
                        new ColumnDiff
                        {
                            Type = SchemaDiffType.ColumnModified,
                            ColumnName = "created_at",
                            OldType = "DATETIME",
                            NewType = "DATETIME(6)"
                        }
                    ]
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("MODIFY COLUMN", statements[0]);
        Assert.Contains("created_at", statements[0]);
    }

    [Fact]
    public void GenerateMigrationSql_MySQL_TINYINT升级INT_使用MODIFY()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableModified,
                    TableName = "stats",
                    ColumnDiffs =
                    [
                        new ColumnDiff
                        {
                            Type = SchemaDiffType.ColumnModified,
                            ColumnName = "count",
                            OldType = "TINYINT",
                            NewType = "INT"
                        }
                    ]
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("MODIFY COLUMN", statements[0]);
        Assert.Contains("count", statements[0]);
    }

    #endregion

    #region MySQL 查询翻译测试

    [Fact]
    public void TranslateUpsert_MySQL_生成ON_DUPLICATE_KEY_UPDATE()
    {
        var translator = new MySqlQueryTranslator();
        var query = new UpsertQuery(
            "users",
            [new FieldAssignment("id", 1), new FieldAssignment("name", "Alice")],
            ["id"],
            UpsertStrategy.DoUpdate,
            [new FieldAssignment("name", "Alice")]);

        var sql = translator.Translate(query);

        Assert.Contains("INSERT INTO `users`", sql);
        Assert.Contains("ON DUPLICATE KEY UPDATE", sql);
        Assert.Contains("`name` = VALUES(`name`)", sql);
    }

    [Fact]
    public void TranslateFullTextSearch_MySQL_生成MATCH_AGAINST()
    {
        var translator = new MySqlQueryTranslator();
        var query = new FullTextSearchQuery("articles", ["title", "body"], "database");

        var sql = translator.Translate(query);

        Assert.Contains("MATCH(`title`, `body`)", sql);
        Assert.Contains("AGAINST", sql);
    }

    [Fact]
    public void TranslateCte_MySQL_生成WITH()
    {
        var translator = new MySqlQueryTranslator();
        var innerQuery = new FindQuery("users");
        var cteDef = new CteDefinition("active_users", innerQuery);
        var mainQuery = new FindQuery("active_users");
        var query = new CteQuery("active_users", [cteDef], mainQuery);

        var sql = translator.Translate(query);

        Assert.Contains("WITH", sql);
        Assert.Contains("active_users", sql);
    }

    [Fact]
    public void TranslateCte_MySQL_递归CTE_生成WITH_RECURSIVE()
    {
        var translator = new MySqlQueryTranslator();
        var innerQuery = new FindQuery("tree");
        var cteDef = new CteDefinition("tree_path", innerQuery, null, true);
        var mainQuery = new FindQuery("tree_path");
        var query = new CteQuery("tree_path", [cteDef], mainQuery);

        var sql = translator.Translate(query);

        Assert.Contains("WITH RECURSIVE", sql);
    }

    [Fact]
    public void TranslateWindow_MySQL_生成ROW_NUMBER()
    {
        var translator = new MySqlQueryTranslator();
        var wf = new WindowFunctionDefinition(
            WindowFunctionKind.RowNumber, "score", "row_num",
            ["score"], [new WindowOrderItem("score", true)]);
        var query = new WindowQuery("scores", [wf]);

        var sql = translator.Translate(query);

        Assert.Contains("ROW_NUMBER()", sql);
        Assert.Contains("OVER", sql);
        Assert.Contains("ORDER BY", sql);
    }

    #endregion

    #region 连接池和预编译语句测试

    [Fact]
    public void MySqlStorageAdapter_构造函数_使用连接池()
    {
        var options = new MySqlConnectOptions
        {
            Host = "localhost",
            Port = 3306,
            Username = "root",
            Password = "test",
            Database = "testdb"
        };

        using var adapter = new MySqlStorageAdapter(options);

        Assert.Equal(StorageBackendKind.MySQL, adapter.Kind);
        Assert.Contains("mysql", adapter.Name);
    }

    [Fact]
    public void MySqlStorageAdapter_预编译语句缓存_初始为空()
    {
        var options = new MySqlConnectOptions
        {
            Host = "localhost",
            Port = 3306,
            Username = "root",
            Password = "test",
            Database = "testdb"
        };

        using var adapter = new MySqlStorageAdapter(options);

        Assert.Equal(0, adapter.PreparedStatementCount);
    }

    [Fact]
    public void MySqlStorageAdapter_清除预编译缓存_不抛异常()
    {
        var options = new MySqlConnectOptions
        {
            Host = "localhost",
            Port = 3306,
            Username = "root",
            Password = "test",
            Database = "testdb"
        };

        using var adapter = new MySqlStorageAdapter(options);
        adapter.ClearPreparedStatementCache();

        Assert.Equal(0, adapter.PreparedStatementCount);
    }

    #endregion
}
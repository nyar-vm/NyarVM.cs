namespace Hermes.Plugin.Sql.Tests;

public sealed class DdlGeneratorTests
{
    [Fact]
    public void GenerateCreateTable_Basic_生成正确SQL()
    {
        var ddl = new DdlGenerator(SqlDialect.Sqlite);
        var modelDef = new ModelDefinition("user", PrimitiveType.I32, new[]
        {
            new FieldDefinition("name", PrimitiveType.Utf8, false),
            new FieldDefinition("age", PrimitiveType.I32, false)
        });

        var schema = new SchemaIR("test", storages: [new StorageDefinition("main", models: [modelDef])]);
        var sql = ddl.GenerateCreateTableSql(modelDef, schema);

        Assert.Contains("CREATE TABLE IF NOT EXISTS \"user\"", sql);
        Assert.Contains("\"name\" TEXT NOT NULL", sql);
        Assert.Contains("\"age\" INTEGER NOT NULL", sql);
    }

    [Fact]
    public void GenerateCreateTable_PrimaryKey_SQLite内联PK()
    {
        var ddl = new DdlGenerator(SqlDialect.Sqlite);
        var modelDef = new ModelDefinition("player", PrimitiveType.I32, new[]
        {
            new FieldDefinition("id", PrimitiveType.I32, false, null, [new("key", [])]),
            new FieldDefinition("score", PrimitiveType.I32, false)
        });

        var schema = new SchemaIR("test", storages: [new StorageDefinition("game", models: [modelDef])]);
        var sql = ddl.GenerateCreateTableSql(modelDef, schema);

        Assert.Contains("INTEGER PRIMARY KEY AUTOINCREMENT", sql);
    }

    [Fact]
    public void GenerateCreateTable_PrimaryKey_MySQL自增PK()
    {
        var ddl = new DdlGenerator(SqlDialect.MySql);
        var modelDef = new ModelDefinition("item", PrimitiveType.I32, new[]
        {
            new FieldDefinition("id", PrimitiveType.I32, false, null, [new("key", [])]),
            new FieldDefinition("label", PrimitiveType.Utf8, false)
        });

        var schema = new SchemaIR("test", storages: [new StorageDefinition("shop", models: [modelDef])]);
        var sql = ddl.GenerateCreateTableSql(modelDef, schema);

        Assert.Contains("PRIMARY KEY (`id`)", sql);
        Assert.Contains("INT AUTO_INCREMENT", sql);
    }

    [Fact]
    public void GenerateCreateTable_PrimaryKey_PostgreSQLSerial()
    {
        var ddl = new DdlGenerator(SqlDialect.PostgreSql);
        var modelDef = new ModelDefinition("account", PrimitiveType.I32, new[]
        {
            new FieldDefinition("id", PrimitiveType.I32, false, null, [new("key", [])]),
            new FieldDefinition("balance", PrimitiveType.F64, false)
        });

        var schema = new SchemaIR("test", storages: [new StorageDefinition("bank", models: [modelDef])]);
        var sql = ddl.GenerateCreateTableSql(modelDef, schema);

        Assert.Contains("\"id\" SERIAL NOT NULL", sql);
    }

    [Fact]
    public void GenerateCreateTable_UniqueKey_生成UNIQUE()
    {
        var ddl = new DdlGenerator(SqlDialect.Sqlite);
        var modelDef = new ModelDefinition("email_list", PrimitiveType.I32, new[]
        {
            new FieldDefinition("email", PrimitiveType.Utf8, false, null, [new("unique", [])])
        });

        var schema = new SchemaIR("test", storages: [new StorageDefinition("app", models: [modelDef])]);
        var sql = ddl.GenerateCreateTableSql(modelDef, schema);

        Assert.Contains("UNIQUE", sql);
    }

    [Fact]
    public void GenerateCreateTable_ForeignKey_生成FK()
    {
        var ddl = new DdlGenerator(SqlDialect.Sqlite);
        var modelDef = new ModelDefinition("order", PrimitiveType.I32, new[]
        {
            new FieldDefinition("id", PrimitiveType.I32, false, null, [new("key", [])]),
            new FieldDefinition("user_id", PrimitiveType.I32, false)
        }, [], new[]
        {
            new FkDefinition("user_id", "id", "user")
        });

        var schema = new SchemaIR("test", storages: [new StorageDefinition("ecom", models: [modelDef])]);
        var sql = ddl.GenerateCreateTableSql(modelDef, schema);

        Assert.Contains("FOREIGN KEY", sql);
        Assert.Contains("REFERENCES \"user\"", sql);
    }

    [Fact]
    public void GenerateCreateTable_Flatten_字段展开()
    {
        var ddl = new DdlGenerator(SqlDialect.Sqlite);
        var addressDef = new ClassDefinition("Address", new[]
        {
            new FieldDefinition("city", PrimitiveType.Utf8, false),
            new FieldDefinition("zipcode", PrimitiveType.Utf8, false)
        }, []);

        var modelDef = new ModelDefinition("company", PrimitiveType.I32, new[]
        {
            new FieldDefinition("name", PrimitiveType.Utf8, false),
            new FieldDefinition("location", new NamedType("Address"), false, null, [new("flatten", [])])
        }, []);

        var schema = new SchemaIR("test", classes: [addressDef],
            storages: [new StorageDefinition("crm", models: [modelDef])]);

        var sql = ddl.GenerateCreateTableSql(modelDef, schema);

        Assert.Contains("\"location_city\"", sql);
        Assert.Contains("\"location_zipcode\"", sql);
    }

    [Fact]
    public void GenerateCreateTable_SnakeCase_命名转换()
    {
        var ddl = new DdlGenerator(SqlDialect.Sqlite);
        var modelDef = new ModelDefinition("UserProfile", PrimitiveType.I32, new[]
        {
            new FieldDefinition("firstName", PrimitiveType.Utf8, false),
            new FieldDefinition("createdAt", PrimitiveType.Utf8, false)
        });

        var schema = new SchemaIR("test", storages: [new StorageDefinition("app", models: [modelDef])]);

        var sql = ddl.GenerateCreateTableSql(modelDef, schema);

        Assert.Contains("CREATE TABLE IF NOT EXISTS \"user_profile\"", sql);
        Assert.Contains("\"first_name\"", sql);
        Assert.Contains("\"created_at\"", sql);
    }

    [Fact]
    public void GenerateDdlStatements_包含所有表()
    {
        var ddl = new DdlGenerator(SqlDialect.Sqlite);
        var m1 = new ModelDefinition("a", PrimitiveType.I32,
            new[] { new FieldDefinition("x", PrimitiveType.I32, false, null, [new("key", [])]) });
        var m2 = new ModelDefinition("b", PrimitiveType.I32,
            new[] { new FieldDefinition("y", PrimitiveType.Utf8, false, null, [new("unique", [])]) });

        var schema = new SchemaIR("test", storages: [new StorageDefinition("s", models: [m1, m2])]);
        var statements = ddl.GenerateDdlStatements(schema);

        Assert.Equal(2, statements.Count);
        Assert.Contains(statements, s => s.Contains("\"a\""));
        Assert.Contains(statements, s => s.Contains("\"b\""));
    }

    [Fact]
    public void GenerateMigrationSql_添加表_生成Note()
    {
        var ddl = new DdlGenerator(SqlDialect.Sqlite);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableAdded,
                    TableName = "new_table"
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("new_table", statements[0]);
        Assert.Contains("TODO: CREATE TABLE", statements[0]);
    }

    [Fact]
    public void GenerateMigrationSql_删除表_生成DROP()
    {
        var ddl = new DdlGenerator(SqlDialect.Sqlite);
        var diff = new SchemaDiffResult
        {
            TableDiffs =
            [
                new TableDiff
                {
                    Type = SchemaDiffType.TableRemoved,
                    TableName = "old_table"
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("DROP TABLE IF EXISTS", statements[0]);
        Assert.Contains("old_table", statements[0]);
    }

    [Fact]
    public void GenerateMigrationSql_添加列_生成ALTER()
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
                            ColumnName = "bio",
                            NewType = "TEXT"
                        }
                    ]
                }
            ]
        };

        var statements = ddl.GenerateMigrationSql(diff);

        Assert.Single(statements);
        Assert.Contains("ALTER TABLE", statements[0]);
        Assert.Contains("ADD COLUMN", statements[0]);
        Assert.Contains("bio", statements[0]);
    }
}
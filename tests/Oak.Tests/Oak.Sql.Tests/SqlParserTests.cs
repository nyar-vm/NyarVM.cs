using Oak.Syntax;

namespace Oak.Sql.Tests;

public sealed class SqlParserTests
{
    [Fact]
    public void Parse_SimpleSelect()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users");

        Assert.True(result.Success);
        Assert.IsType<SelectStatement>(result.Value);
    }

    [Fact]
    public void Parse_SelectWithColumns()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT id, name, email FROM users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Equal(3, stmt.Columns.Count);
    }

    [Fact]
    public void Parse_SelectDistinct()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT DISTINCT name FROM users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.True(stmt.Distinct);
    }

    [Fact]
    public void Parse_SelectWithAlias()
    {
        using var _ = Assert.Multiple(() =>
        {
            var parser = new SqlParser();
            var result = parser.Parse("SELECT name AS n FROM users");
            Assert.True(result.Success);
            var stmt = Assert.IsType<SelectStatement>(result.Value);
            Assert.Equal("n", stmt.Columns[0].Alias);
        });
    }

    [Fact]
    public void Parse_SelectWithAliasNoAs()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT name n FROM users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Equal("n", stmt.Columns[0].Alias);
    }

    [Fact]
    public void Parse_WhereClause()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE id = 1");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.NotNull(stmt.Where);
    }

    [Fact]
    public void Parse_WhereWithAndOr()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE id = 1 AND name = 'Alice' OR active = TRUE");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.NotNull(stmt.Where);
    }

    [Fact]
    public void Parse_BooleanLiteralTrue()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE active = TRUE");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.NotNull(stmt.Where);
        Assert.IsType<BinaryExpr>(stmt.Where);
        var bin = (BinaryExpr)stmt.Where!;
        Assert.IsType<LiteralValue>(bin.Right);
        var lit = (LiteralValue)bin.Right;
        Assert.Equal(SqlTokenType.True, lit.type);
    }

    [Fact]
    public void Parse_BooleanLiteralFalse()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE deleted = FALSE");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_NotBooleanExpr()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE NOT deleted");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_BlockComment()
    {
        var parser = new SqlParser();
        var result = parser.Parse("/* 这是一个块注释 */ SELECT * FROM users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Equal("users", stmt.From!.Name);
    }

    [Fact]
    public void Parse_BlockCommentInline()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT /* 列名 */ id, name FROM users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Equal(2, stmt.Columns.Count);
    }

    [Fact]
    public void Parse_NestedBlockComment()
    {
        var parser = new SqlParser();
        var result = parser.Parse("/* 外层 /* 内层 */ 外层 */\nSELECT * FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_InsertIntoValues()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT INTO users (id, name) VALUES (1, 'Alice')");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.Equal("users", stmt.Table);
        Assert.Equal(2, stmt.Columns.Count);
        Assert.Single(stmt.ValuesRows);
        Assert.Equal(2, stmt.ValuesRows[0].Count);
    }

    [Fact]
    public void Parse_InsertMultiRows()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT INTO users (id, name) VALUES (1, 'Alice'), (2, 'Bob')");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.Equal(2, stmt.ValuesRows.Count);
    }

    [Fact]
    public void Parse_InsertDefaultValues()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT INTO users DEFAULT VALUES");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.True(stmt.IsDefaultValues);
    }

    [Fact]
    public void Parse_InsertOrReplace()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT OR REPLACE INTO users (id, name) VALUES (1, 'Alice')");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.Equal(InsertKind.InsertOrReplace, stmt.Kind);
    }

    [Fact]
    public void Parse_InsertOrIgnore()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT OR IGNORE INTO users (id, name) VALUES (1, 'Alice')");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.Equal(InsertKind.InsertOrIgnore, stmt.Kind);
    }

    [Fact]
    public void Parse_ReplaceInto()
    {
        var parser = new SqlParser();
        var result = parser.Parse("REPLACE INTO users (id, name) VALUES (1, 'Alice')");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.Equal(InsertKind.Replace, stmt.Kind);
    }

    [Fact]
    public void Parse_InsertIntoSelect()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT INTO archive SELECT * FROM users WHERE active = FALSE");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.Equal("archive", stmt.Table);
        Assert.NotNull(stmt.SelectSource);
        Assert.Equal("users", stmt.SelectSource!.From!.Name);
    }

    [Fact]
    public void Parse_InsertIntoSelectWithColumns()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT INTO archive (id, name) SELECT id, name FROM users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.Equal(2, stmt.Columns.Count);
        Assert.NotNull(stmt.SelectSource);
        Assert.Equal(2, stmt.SelectSource!.Columns.Count);
    }

    [Fact]
    public void Parse_InsertWithReturning()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT INTO users (name) VALUES ('Alice') RETURNING id");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.NotNull(stmt.Returning);
        Assert.Single(stmt.Returning);
    }

    [Fact]
    public void Parse_UpdateWithReturning()
    {
        var parser = new SqlParser();
        var result = parser.Parse("UPDATE users SET name = 'Bob' WHERE id = 1 RETURNING id, name");

        Assert.True(result.Success);
        var stmt = Assert.IsType<UpdateStatement>(result.Value);
        Assert.NotNull(stmt.Returning);
        Assert.Equal(2, stmt.Returning!.Count);
    }

    [Fact]
    public void Parse_DeleteWithReturning()
    {
        var parser = new SqlParser();
        var result = parser.Parse("DELETE FROM users WHERE id = 1 RETURNING id");

        Assert.True(result.Success);
        var stmt = Assert.IsType<DeleteStatement>(result.Value);
        Assert.NotNull(stmt.Returning);
    }

    [Fact]
    public void Parse_OnConflictDoNothing()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT INTO users (id, name) VALUES (1, 'Alice') ON CONFLICT DO NOTHING");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.NotNull(stmt.OnConflict);
        Assert.Equal(ConflictAction.Ignore, stmt.OnConflict!.Action);
    }

    [Fact]
    public void Parse_OnConflictDoUpdate()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT INTO users (id, name) VALUES (1, 'Alice') ON CONFLICT (id) DO UPDATE SET name = excluded.name");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.NotNull(stmt.OnConflict);
        Assert.Equal(ConflictAction.Replace, stmt.OnConflict!.Action);
        Assert.Single(stmt.OnConflict.ConflictColumns!);
        Assert.Equal("id", stmt.OnConflict.ConflictColumns![0]);
        Assert.Single(stmt.OnConflict.UpdateAssignments!);
        Assert.Equal("name", stmt.OnConflict.UpdateAssignments![0].Column);
    }

    [Fact]
    public void Parse_OnConflictDoUpdateWithWhere()
    {
        var parser = new SqlParser();
        var result = parser.Parse("INSERT INTO users (id, name) VALUES (1, 'Alice') ON CONFLICT (id) DO UPDATE SET name = excluded.name WHERE excluded.name IS NOT NULL");

        Assert.True(result.Success);
        var stmt = Assert.IsType<InsertStatement>(result.Value);
        Assert.NotNull(stmt.OnConflict!.UpdateWhere);
    }

    [Fact]
    public void Parse_UpdateStatement()
    {
        var parser = new SqlParser();
        var result = parser.Parse("UPDATE users SET name = 'Bob' WHERE id = 1");

        Assert.True(result.Success);
        var stmt = Assert.IsType<UpdateStatement>(result.Value);
        Assert.Equal("users", stmt.Table);
        Assert.Single(stmt.Assignments);
        Assert.NotNull(stmt.Where);
    }

    [Fact]
    public void Parse_DeleteStatement()
    {
        var parser = new SqlParser();
        var result = parser.Parse("DELETE FROM users WHERE id = 1");

        Assert.True(result.Success);
        var stmt = Assert.IsType<DeleteStatement>(result.Value);
        Assert.Equal("users", stmt.Table);
        Assert.NotNull(stmt.Where);
    }

    [Fact]
    public void Parse_DropTable()
    {
        var parser = new SqlParser();
        var result = parser.Parse("DROP TABLE users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<DropTableStatement>(result.Value);
        Assert.Equal("users", stmt.Table);
        Assert.False(stmt.IfExists);
    }

    [Fact]
    public void Parse_DropTableIfExists()
    {
        var parser = new SqlParser();
        var result = parser.Parse("DROP TABLE IF EXISTS users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<DropTableStatement>(result.Value);
        Assert.True(stmt.IfExists);
    }

    #region ALTER TABLE

    [Fact]
    public void Parse_AlterTableAddColumn()
    {
        var parser = new SqlParser();
        var result = parser.Parse("ALTER TABLE users ADD COLUMN age INTEGER NOT NULL DEFAULT 0");

        Assert.True(result.Success);
        var stmt = Assert.IsType<AlterTableStatement>(result.Value);
        Assert.Equal("users", stmt.Table);
        var action = Assert.IsType<AddColumnAction>(stmt.Action);
        Assert.Equal("age", action.Column.Name);
        Assert.Equal("INTEGER", action.Column.type);
        Assert.True(action.Column.IsNotNull);
    }

    [Fact]
    public void Parse_AlterTableRenameColumn()
    {
        var parser = new SqlParser();
        var result = parser.Parse("ALTER TABLE users RENAME COLUMN name TO full_name");

        Assert.True(result.Success);
        var stmt = Assert.IsType<AlterTableStatement>(result.Value);
        var action = Assert.IsType<RenameColumnAction>(stmt.Action);
        Assert.Equal("name", action.OldName);
        Assert.Equal("full_name", action.NewName);
    }

    [Fact]
    public void Parse_AlterTableRenameTable()
    {
        var parser = new SqlParser();
        var result = parser.Parse("ALTER TABLE users RENAME TO accounts");

        Assert.True(result.Success);
        var stmt = Assert.IsType<AlterTableStatement>(result.Value);
        var action = Assert.IsType<RenameTableAction>(stmt.Action);
        Assert.Equal("accounts", action.NewName);
    }

    [Fact]
    public void Parse_AlterTableDropColumn()
    {
        var parser = new SqlParser();
        var result = parser.Parse("ALTER TABLE users DROP COLUMN age");

        Assert.True(result.Success);
        var stmt = Assert.IsType<AlterTableStatement>(result.Value);
        var action = Assert.IsType<DropColumnAction>(stmt.Action);
        Assert.Equal("age", action.ColumnName);
    }

    #endregion

    #region CREATE INDEX / DROP INDEX

    [Fact]
    public void Parse_CreateIndex()
    {
        var parser = new SqlParser();
        var result = parser.Parse("CREATE INDEX idx_users_name ON users (name)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CreateIndexStatement>(result.Value);
        Assert.False(stmt.IsUnique);
        Assert.False(stmt.IfNotExists);
        Assert.Equal("idx_users_name", stmt.Name);
        Assert.Equal("users", stmt.Table);
        Assert.Single(stmt.Columns);
        Assert.Equal("name", stmt.Columns[0].Column);
    }

    [Fact]
    public void Parse_CreateUniqueIndex()
    {
        var parser = new SqlParser();
        var result = parser.Parse("CREATE UNIQUE INDEX idx_users_email ON users (email)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CreateIndexStatement>(result.Value);
        Assert.True(stmt.IsUnique);
    }

    [Fact]
    public void Parse_CreateIndexIfNotExists()
    {
        var parser = new SqlParser();
        var result = parser.Parse("CREATE INDEX IF NOT EXISTS idx_name ON users (name)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CreateIndexStatement>(result.Value);
        Assert.True(stmt.IfNotExists);
    }

    [Fact]
    public void Parse_CreateIndexMultiColumn()
    {
        var parser = new SqlParser();
        var result = parser.Parse("CREATE INDEX idx_compound ON users (name, age DESC)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CreateIndexStatement>(result.Value);
        Assert.Equal(2, stmt.Columns.Count);
        Assert.True(stmt.Columns[1].Descending);
    }

    [Fact]
    public void Parse_DropIndex()
    {
        var parser = new SqlParser();
        var result = parser.Parse("DROP INDEX idx_old");

        Assert.True(result.Success);
        var stmt = Assert.IsType<DropIndexStatement>(result.Value);
        Assert.Equal("idx_old", stmt.Name);
    }

    [Fact]
    public void Parse_DropIndexIfExists()
    {
        var parser = new SqlParser();
        var result = parser.Parse("DROP INDEX IF EXISTS idx_old");

        Assert.True(result.Success);
        var stmt = Assert.IsType<DropIndexStatement>(result.Value);
        Assert.True(stmt.IfExists);
    }

    #endregion

    #region 子查询与表达式

    [Fact]
    public void Parse_SubqueryExpr()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE id = (SELECT MAX(id) FROM users)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.NotNull(stmt.Where);
    }

    [Fact]
    public void Parse_ExistsExpr()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE EXISTS (SELECT 1 FROM orders WHERE orders.user_id = users.id)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.NotNull(stmt.Where);
    }

    [Fact]
    public void Parse_NotExistsExpr()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE NOT EXISTS (SELECT 1 FROM orders WHERE orders.user_id = users.id)");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_InSubquery()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE id IN (SELECT user_id FROM orders)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.NotNull(stmt.Where);
    }

    [Fact]
    public void Parse_NotInSubquery()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE id NOT IN (SELECT user_id FROM blocked)");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_CaseWhenExpr()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT CASE WHEN active THEN '是' ELSE '否' END FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_CaseWhenMultipleExpr()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT CASE WHEN score >= 90 THEN 'A' WHEN score >= 80 THEN 'B' ELSE 'F' END FROM grades");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_CastExpr()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT CAST(age AS INTEGER) FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_CastExprWithParamType()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT CAST(age AS VARCHAR(10)) FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_CollateExpr()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE name COLLATE nocase = 'alice'");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_CollateOnColumn()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT name COLLATE nocase FROM users");

        Assert.True(result.Success);
    }

    #endregion

    #region 复合查询

    [Fact]
    public void Parse_Union()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT name FROM customers UNION SELECT name FROM suppliers");

        Assert.True(result.Success);
        Assert.IsType<CompoundSelectStatement>(result.Value);
    }

    [Fact]
    public void Parse_UnionAll()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT name FROM a UNION ALL SELECT name FROM b");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CompoundSelectStatement>(result.Value);
        Assert.Equal(CompoundOperator.UnionAll, stmt.Operator);
    }

    [Fact]
    public void Parse_Intersect()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT id FROM a INTERSECT SELECT id FROM b");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CompoundSelectStatement>(result.Value);
        Assert.Equal(CompoundOperator.Intersect, stmt.Operator);
    }

    [Fact]
    public void Parse_Except()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT id FROM a EXCEPT SELECT id FROM b");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CompoundSelectStatement>(result.Value);
        Assert.Equal(CompoundOperator.Except, stmt.Operator);
    }

    #endregion

    #region JOIN 类型

    [Fact]
    public void Parse_FullOuterJoin()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users FULL OUTER JOIN orders ON users.id = orders.user_id");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Single(stmt.Joins);
        Assert.Equal(SqlTokenType.Full, stmt.Joins[0].JoinType);
    }

    [Fact]
    public void Parse_CrossJoin()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users CROSS JOIN settings");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Single(stmt.Joins);
        Assert.Equal(SqlTokenType.Cross, stmt.Joins[0].JoinType);
    }

    [Fact]
    public void Parse_NaturalJoin()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users NATURAL JOIN profiles");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Single(stmt.Joins);
        Assert.Equal(SqlTokenType.Natural, stmt.Joins[0].JoinType);
    }

    #endregion

    #region 列约束增强

    [Fact]
    public void Parse_CreateTableWithAutoincrement()
    {
        var parser = new SqlParser();
        var result = parser.Parse("CREATE TABLE users (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CreateTableStatement>(result.Value);
        Assert.True(stmt.Columns[0].IsPrimaryKey);
        Assert.True(stmt.Columns[0].IsAutoincrement);
    }

    [Fact]
    public void Parse_CreateTableWithUniqueConstraint()
    {
        var parser = new SqlParser();
        var result = parser.Parse("CREATE TABLE users (id INTEGER PRIMARY KEY, email TEXT UNIQUE)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CreateTableStatement>(result.Value);
        Assert.True(stmt.Columns[1].IsUnique);
    }

    [Fact]
    public void Parse_CreateTableWithCheckConstraint()
    {
        var parser = new SqlParser();
        var result = parser.Parse("CREATE TABLE users (id INTEGER, age INTEGER CHECK (age >= 0))");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CreateTableStatement>(result.Value);
        Assert.NotNull(stmt.Columns[1].CheckExpression);
    }

    [Fact]
    public void Parse_CreateTableWithCollate()
    {
        var parser = new SqlParser();
        var result = parser.Parse("CREATE TABLE users (id INTEGER, name TEXT COLLATE nocase)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CreateTableStatement>(result.Value);
        Assert.Equal("nocase", stmt.Columns[1].Collate);
    }

    [Fact]
    public void Parse_CreateTableWithMultipleTypes()
    {
        var parser = new SqlParser();
        var result = parser.Parse("CREATE TABLE products (id INTEGER PRIMARY KEY, price DECIMAL(10,2), qty BIGINT, flag BOOL, data BLOB)");

        Assert.True(result.Success);
        var stmt = Assert.IsType<CreateTableStatement>(result.Value);
        Assert.Equal(5, stmt.Columns.Count);
        Assert.Equal("INTEGER", stmt.Columns[0].type);
        Assert.Equal("DECIMAL(10,2)", stmt.Columns[1].type);
        Assert.Equal("BIGINT", stmt.Columns[2].type);
        Assert.Equal("BOOL", stmt.Columns[3].type);
        Assert.Equal("BLOB", stmt.Columns[4].type);
    }

    #endregion

    #region NOT LIKE / NOT BETWEEN / GLOB

    [Fact]
    public void Parse_NotLike()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE name NOT LIKE '%admin%'");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_NotBetween()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE age NOT BETWEEN 10 AND 20");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_Glob()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE name GLOB 'A*'");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_NotGlob()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users WHERE name NOT GLOB 'A*'");

        Assert.True(result.Success);
    }

    #endregion

    #region 算术和位运算符

    [Fact]
    public void Parse_ModuloOperator()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT id % 2 FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_ConcatOperator()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT first_name || ' ' || last_name AS full_name FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_BitwiseAndOperator()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT flags & 1 FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_BitwiseOrOperator()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT flags | 4 FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_LeftShiftOperator()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT value << 2 FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_RightShiftOperator()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT value >> 1 FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_BitwiseNotOperator()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT ~flags FROM users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.IsType<UnaryExpr>(stmt.Columns[0].Expression);
    }

    #endregion

    #region NULLS FIRST / LAST

    [Fact]
    public void Parse_OrderByNullsFirst()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users ORDER BY name ASC NULLS FIRST");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Single(stmt.OrderBy);
        Assert.True(stmt.OrderBy[0].NullsFirst);
    }

    [Fact]
    public void Parse_OrderByNullsLast()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT * FROM users ORDER BY name DESC NULLS LAST");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Single(stmt.OrderBy);
        Assert.True(stmt.OrderBy[0].NullsLast);
    }

    #endregion

    #region 类型关键字作为列名

    [Fact]
    public void Parse_TypeKeywordAsColumnName()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT id, integer, text FROM users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Equal(3, stmt.Columns.Count);
    }

    [Fact]
    public void Parse_KeywordAsIdentifier()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT id, index FROM users");

        Assert.True(result.Success);
        var stmt = Assert.IsType<SelectStatement>(result.Value);
        Assert.Equal(2, stmt.Columns.Count);
    }

    #endregion

    #region 空参数函数

    [Fact]
    public void Parse_FunctionWithNoArgs()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT NOW(), RANDOM() FROM users");

        Assert.True(result.Success);
    }

    [Fact]
    public void Parse_AggregateWithDistinct()
    {
        var parser = new SqlParser();
        var result = parser.Parse("SELECT COUNT(DISTINCT name) FROM users");

        Assert.True(result.Success);
    }

    #endregion
}

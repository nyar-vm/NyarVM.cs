using System.Text;
using Std.Data.Text.Diagnostics;
using Std.Data.Text.Parsing;

namespace Std.Data.Text.Sql;

public sealed class SqlParser
{
    private DiagnosticSink? _diagnostics;
    private int _position;
    private IReadOnlyList<SqlToken> _tokens = [];

    private SqlToken _current => _position < _tokens.Count ? _tokens[_position] : _tokens[^1];

    private SqlTokenType peek(int offset = 0)
    {
        var idx = _position + offset;
        return idx < _tokens.Count ? _tokens[idx].type : SqlTokenType.end_of_file;
    }

    public ParseResult<SqlNode> parse(string source, DiagnosticSink? diagnostics = null)
    {
        var lexer = new SqlLexer();
        _tokens = lexer.tokenize(source, diagnostics);
        _position = 0;
        _diagnostics = diagnostics;

        var statements = new List<SqlNode>();

        while (!is_at_end())
        {
            var stmt = parse_statement();
            if (stmt is not null) statements.Add(stmt);

            while (match(SqlTokenType.semicolon))
            {
            }
        }

        if (statements.Count == 0)
        {
            _diagnostics?.report_error(string.Empty, default, 1, "未解析到有效 SQL 语句");
            return ParseResult<SqlNode>.fail(_diagnostics?.messages ?? []);
        }

        if (statements.Count == 1) return ParseResult<SqlNode>.ok(statements[0], _diagnostics?.messages);

        return ParseResult<SqlNode>.ok(statements[0], _diagnostics?.messages);
    }

    private SqlNode? parse_statement()
    {
        if (check(SqlTokenType.select)) return parse_select_or_compound();

        if (check(SqlTokenType.insert)) return parse_insert();

        if (check(SqlTokenType.replace)) return parse_insert(InsertKind.replace);

        if (check(SqlTokenType.upsert)) return parse_insert(InsertKind.upsert);

        if (check(SqlTokenType.update)) return parse_update();

        if (check(SqlTokenType.delete)) return parse_delete();

        if (check(SqlTokenType.alter)) return parse_alter_table();

        if (check(SqlTokenType.drop))
        {
            if (peek(1) == SqlTokenType.table) return parse_drop_table();

            if (peek(1) == SqlTokenType.index) return parse_drop_index();

            if (peek(1) == SqlTokenType.materialized) return parse_drop_materialized_view();

            if (peek(1) == SqlTokenType.prepare) return parse_deallocate_prepare();
        }

        if (check(SqlTokenType.deallocate)) return parse_deallocate_prepare();

        if (check(SqlTokenType.create))
        {
            var next = peek(1);
            if (next == SqlTokenType.table) return parse_create_table();

            if (next == SqlTokenType.function) return parse_create_function();

            if (next == SqlTokenType.procedure) return parse_create_procedure();

            if (next == SqlTokenType.materialized) return parse_create_materialized_view();

            if (next is SqlTokenType.unique or SqlTokenType.index) return parse_create_index();
        }

        if (check(SqlTokenType.refresh)) return parse_refresh_materialized_view();

        if (check(SqlTokenType.call)) return parse_call_stmt();

        if (check(SqlTokenType.show)) return parse_show_tables();

        if (check(SqlTokenType.describe)) return parse_describe_table();

        if (check(SqlTokenType.prepare)) return parse_prepare();

        if (check(SqlTokenType.execute)) return parse_execute();

        _diagnostics?.report_error(string.Empty, default,
            100, $"意外的词法单元：{_current.type}");

        advance();
        return null;
    }

    private SqlNode parse_select_or_compound()
    {
        var select = parse_select();

        while (!is_at_end())
        {
            if (match(SqlTokenType.union))
            {
                var all = match(SqlTokenType.all);
                var right = parse_select();
                return new CompoundSelectStatement(
                    all ? CompoundOperator.union_all : CompoundOperator.union, select, right);
            }

            if (match(SqlTokenType.intersect))
                return new CompoundSelectStatement(CompoundOperator.intersect, select, parse_select());

            if (match(SqlTokenType.except))
                return new CompoundSelectStatement(CompoundOperator.except, select, parse_select());

            break;
        }

        return select;
    }

    #region ALTER TABLE

    private AlterTableStatement parse_alter_table()
    {
        consume(SqlTokenType.alter, 155, "期望 'ALTER'");
        consume(SqlTokenType.table, 156, "期望 'TABLE'");

        var table = consume_identifier(157, "期望表名");

        AlterTableAction action;
        if (match(SqlTokenType.add))
        {
            match(SqlTokenType.column);
            action = new AddColumnAction(parse_column_def());
        }
        else if (match(SqlTokenType.drop))
        {
            consume(SqlTokenType.column, 204, "期望 'COLUMN'");
            var colName = consume_identifier(205, "期望列名");
            action = new DropColumnAction(colName);
        }
        else if (match(SqlTokenType.rename))
        {
            if (match(SqlTokenType.column))
            {
                var oldName = consume_identifier(158, "期望旧列名");
                consume(SqlTokenType.to, 159, "期望 'TO'");
                var newName = consume_identifier(160, "期望新列名");
                action = new RenameColumnAction(oldName, newName);
            }
            else
            {
                consume(SqlTokenType.to, 159, "期望 'TO'");
                var newName = consume_identifier(161, "期望新表名");
                action = new RenameTableAction(newName);
            }
        }
        else
        {
            _diagnostics?.report_error(string.Empty, default,
                162, "ALTER TABLE 后期望 ADD、DROP 或 RENAME");
            action = new RenameTableAction(string.Empty);
        }

        return new AlterTableStatement(table, action);
    }

    #endregion

    #region DELETE

    private DeleteStatement parse_delete()
    {
        consume(SqlTokenType.delete, 122, "期望 'DELETE'");
        consume(SqlTokenType.from, 123, "期望 'FROM'");

        var table = consume_identifier(124, "期望表名");

        SqlExpression? where = null;
        if (match(SqlTokenType.where)) where = parse_expression();

        var returning = parse_returning_clause();

        return new DeleteStatement(table, where, returning);
    }

    #endregion

    #region DROP TABLE

    private DropTableStatement parse_drop_table()
    {
        consume(SqlTokenType.drop, 132, "期望 'DROP'");
        consume(SqlTokenType.table, 133, "期望 'TABLE'");

        var ifExists = false;
        if (match(SqlTokenType.@if))
        {
            consume(SqlTokenType.exists, 134, "期望 'EXISTS'");
            ifExists = true;
        }

        var table = consume_identifier(135, "期望表名");

        return new DropTableStatement(table, ifExists);
    }

    #endregion

    #region SELECT

    private SelectStatement parse_select()
    {
        consume(SqlTokenType.select, 101, "期望 'SELECT'");

        var distinct = match(SqlTokenType.distinct);
        var columns = parse_column_list();

        SqlTableRef? from = null;
        if (match(SqlTokenType.from)) from = parse_table_ref();

        var joins = new List<SqlTableRef>();
        while (is_join_keyword()) joins.Add(parse_join());

        SqlExpression? where = null;
        if (match(SqlTokenType.where)) where = parse_expression();

        var groupBy = new List<SqlExpression>();
        if (match(SqlTokenType.group))
        {
            consume(SqlTokenType.by, 102, "期望 'BY'");
            groupBy = [.. parse_expression_list()];
        }

        SqlExpression? having = null;
        if (match(SqlTokenType.having)) having = parse_expression();

        var orderBy = new List<OrderByItem>();
        if (match(SqlTokenType.order))
        {
            consume(SqlTokenType.by, 103, "期望 'BY'");
            orderBy = [.. parse_order_by_list()];
        }

        int? limit = null;
        if (match(SqlTokenType.limit)) limit = parse_integer_value();

        int? offset = null;
        if (match(SqlTokenType.offset)) offset = parse_integer_value();

        return new SelectStatement(columns, from, where, joins, groupBy, having, orderBy, limit, offset, distinct);
    }

    private IReadOnlyList<SqlColumn> parse_column_list()
    {
        var columns = new List<SqlColumn> { parse_column() };

        while (match(SqlTokenType.comma)) columns.Add(parse_column());

        return columns;
    }

    private SqlColumn parse_column()
    {
        var expr = parse_expression();
        string? alias = null;

        if (match(SqlTokenType.@as))
            alias = consume_identifier(104, "期望别名");
        else if (check(SqlTokenType.identifier) && !is_keyword(_current.type)) alias = advance().text;

        return new SqlColumn(expr, alias);
    }

    private SqlTableRef parse_table_ref()
    {
        var name = consume_identifier(105, "期望表名");
        string? alias = null;

        if (match(SqlTokenType.@as))
            alias = consume_identifier(106, "期望别名");
        else if (check(SqlTokenType.identifier) && !is_keyword(_current.type)) alias = advance().text;

        return new SqlTableRef(name, alias);
    }

    private bool is_join_keyword()
    {
        return check(SqlTokenType.join) || check(SqlTokenType.inner) ||
               check(SqlTokenType.left) || check(SqlTokenType.right) ||
               check(SqlTokenType.full) || check(SqlTokenType.cross) ||
               check(SqlTokenType.natural);
    }

    private SqlTableRef parse_join()
    {
        var joinType = SqlTokenType.join;

        if (match(SqlTokenType.natural))
        {
            joinType = SqlTokenType.natural;
        }
        else if (match(SqlTokenType.cross))
        {
            joinType = SqlTokenType.cross;
        }
        else if (match(SqlTokenType.inner))
        {
            joinType = SqlTokenType.inner;
        }
        else if (match(SqlTokenType.full))
        {
            joinType = SqlTokenType.full;
            match(SqlTokenType.outer);
        }
        else if (match(SqlTokenType.left))
        {
            joinType = SqlTokenType.left;
            match(SqlTokenType.outer);
        }
        else if (match(SqlTokenType.right))
        {
            joinType = SqlTokenType.right;
            match(SqlTokenType.outer);
        }

        consume(SqlTokenType.join, 107, "期望 'JOIN'");

        var table = parse_table_ref();

        SqlExpression? onCondition = null;
        if (match(SqlTokenType.on)) onCondition = parse_expression();

        return new SqlTableRef(table.name, table.alias, joinType, onCondition);
    }

    private IReadOnlyList<OrderByItem> parse_order_by_list()
    {
        var items = new List<OrderByItem> { parse_order_by_item() };

        while (match(SqlTokenType.comma)) items.Add(parse_order_by_item());

        return items;
    }

    private OrderByItem parse_order_by_item()
    {
        var expr = parse_expression();
        var desc = match(SqlTokenType.desc);
        if (!desc) match(SqlTokenType.asc);

        var nullsFirst = false;
        var nullsLast = false;
        if (match(SqlTokenType.@null))
        {
            if (_current.text.Equals("FIRST", StringComparison.OrdinalIgnoreCase))
            {
                nullsFirst = true;
                advance();
            }
            else if (_current.text.Equals("LAST", StringComparison.OrdinalIgnoreCase))
            {
                nullsLast = true;
                advance();
            }
        }

        return new OrderByItem(expr, desc, nullsFirst, nullsLast);
    }

    #endregion

    #region CREATE INDEX / DROP INDEX

    private CreateIndexStatement parse_create_index()
    {
        consume(SqlTokenType.create, 163, "期望 'CREATE'");

        var isUnique = match(SqlTokenType.unique);

        consume(SqlTokenType.index, 164, "期望 'INDEX'");

        var ifNotExists = false;
        if (match(SqlTokenType.@if))
        {
            consume(SqlTokenType.not, 165, "期望 'NOT'");
            consume(SqlTokenType.exists, 166, "期望 'EXISTS'");
            ifNotExists = true;
        }

        var name = consume_identifier(167, "期望索引名");

        consume(SqlTokenType.on, 168, "期望 'ON'");

        var table = consume_identifier(169, "期望表名");

        consume(SqlTokenType.left_paren, 170, "期望 '('");

        var columns = new List<(string Column, bool Descending)>
        {
            (consume_identifier(171, "期望列名"), match(SqlTokenType.desc))
        };
        while (match(SqlTokenType.comma))
        {
            var col = consume_identifier(171, "期望列名");
            var desc = match(SqlTokenType.desc);
            columns.Add((col, desc));
        }

        consume(SqlTokenType.right_paren, 172, "期望 ')'");

        return new CreateIndexStatement(isUnique, ifNotExists, name, table, columns);
    }

    private DropIndexStatement parse_drop_index()
    {
        consume(SqlTokenType.drop, 173, "期望 'DROP'");
        consume(SqlTokenType.index, 174, "期望 'INDEX'");

        var ifExists = false;
        if (match(SqlTokenType.@if))
        {
            consume(SqlTokenType.exists, 175, "期望 'EXISTS'");
            ifExists = true;
        }

        var name = consume_identifier(176, "期望索引名");

        return new DropIndexStatement(name, ifExists);
    }


    /// <summary>
    ///     解析 RETURNING 子句（INSERT/UPDATE/DELETE 后可选）
    /// </summary>
    private IReadOnlyList<SqlColumn>? parse_returning_clause()
    {
        if (!match(SqlTokenType.returning)) return null;

        return parse_column_list();
    }

    #endregion

    #region INSERT

    private InsertStatement parse_insert(InsertKind kind = InsertKind.insert)
    {
        if (kind == InsertKind.replace)
        {
            consume(SqlTokenType.replace, 108, "期望 'REPLACE'");
        }
        else if (kind == InsertKind.upsert)
        {
            consume(SqlTokenType.upsert, 108, "期望 'UPSERT'");
        }
        else
        {
            consume(SqlTokenType.insert, 108, "期望 'INSERT'");
            if (match(SqlTokenType.or))
            {
                if (match(SqlTokenType.replace))
                    kind = InsertKind.insert_or_replace;
                else if (match(SqlTokenType.ignore)) kind = InsertKind.insert_or_ignore;
            }
        }

        consume(SqlTokenType.into, 109, "期望 'INTO'");

        var table = consume_identifier(110, "期望表名");

        var columns = new List<string>();
        if (match(SqlTokenType.left_paren))
        {
            columns.Add(consume_identifier(111, "期望列名"));
            while (match(SqlTokenType.comma)) columns.Add(consume_identifier(112, "期望列名"));
            consume(SqlTokenType.right_paren, 113, "期望 ')'");
        }

        SelectStatement? selectSource = null;
        IReadOnlyList<IReadOnlyList<SqlExpression>> valueRows = [];
        var isDefault = false;

        if (match(SqlTokenType.@default))
        {
            consume(SqlTokenType.values, 114, "期望 'VALUES'");
            isDefault = true;
        }
        else if (check(SqlTokenType.select))
        {
            selectSource = parse_select_raw();
        }
        else if (match(SqlTokenType.values))
        {
            var rows = new List<IReadOnlyList<SqlExpression>> { parse_value_row() };
            while (match(SqlTokenType.comma)) rows.Add(parse_value_row());
            valueRows = rows;
        }

        OnConflictClause? onConflict = null;
        if (match(SqlTokenType.on))
            if (check(SqlTokenType.conflict))
            {
                advance();
                onConflict = ParseOnConflictClause();
            }

        var returning = parse_returning_clause();

        return new InsertStatement(table, columns, valueRows, isDefault, kind, selectSource, returning, onConflict);
    }

    private OnConflictClause ParseOnConflictClause()
    {
        IReadOnlyList<string>? conflictColumns = null;

        if (match(SqlTokenType.left_paren))
        {
            var cols = new List<string> { consume_identifier(206, "期望冲突列名") };
            while (match(SqlTokenType.comma)) cols.Add(consume_identifier(206, "期望冲突列名"));
            consume(SqlTokenType.right_paren, 207, "期望 ')'");
            conflictColumns = cols;
        }

        ConflictAction? action = null;
        IReadOnlyList<(string, SqlExpression)>? updateAssignments = null;
        SqlExpression? updateWhere = null;

        if (match(SqlTokenType.@do))
        {
            if (match(SqlTokenType.nothing))
            {
                action = ConflictAction.ignore;
            }
            else if (match(SqlTokenType.update))
            {
                consume(SqlTokenType.set, 208, "期望 'SET'");
                action = ConflictAction.replace;

                var assignments = new List<(string, SqlExpression)> { parse_assignment() };
                while (match(SqlTokenType.comma)) assignments.Add(parse_assignment());
                updateAssignments = assignments;

                if (match(SqlTokenType.where)) updateWhere = parse_expression();
            }
        }

        return new OnConflictClause(conflictColumns, action, updateAssignments, updateWhere);
    }

    private IReadOnlyList<SqlExpression> parse_value_row()
    {
        consume(SqlTokenType.left_paren, 115, "期望 '('");
        var values = new List<SqlExpression> { parse_expression() };
        while (match(SqlTokenType.comma)) values.Add(parse_expression());
        consume(SqlTokenType.right_paren, 116, "期望 ')'");
        return values;
    }

    #endregion

    #region UPDATE

    private UpdateStatement parse_update()
    {
        consume(SqlTokenType.update, 117, "期望 'UPDATE'");

        var table = consume_identifier(118, "期望表名");
        consume(SqlTokenType.set, 119, "期望 'SET'");

        var assignments = new List<(string Column, SqlExpression Value)> { parse_assignment() };
        while (match(SqlTokenType.comma)) assignments.Add(parse_assignment());

        SqlExpression? where = null;
        if (match(SqlTokenType.where)) where = parse_expression();

        var returning = parse_returning_clause();

        return new UpdateStatement(table, assignments, where, returning);
    }

    private (string Column, SqlExpression Value) parse_assignment()
    {
        var column = consume_identifier(120, "期望列名");
        consume(SqlTokenType.equal, 121, "期望 '='");
        var value = parse_expression();
        return (column, value);
    }

    #endregion

    #region CREATE TABLE

    private CreateTableStatement parse_create_table()
    {
        consume(SqlTokenType.create, 125, "期望 'CREATE'");
        consume(SqlTokenType.table, 126, "期望 'TABLE'");

        var ifNotExists = false;
        if (match(SqlTokenType.@if))
        {
            consume(SqlTokenType.not, 127, "期望 'NOT'");
            consume(SqlTokenType.exists, 128, "期望 'EXISTS'");
            ifNotExists = true;
        }

        var table = consume_identifier(129, "期望表名");
        consume(SqlTokenType.left_paren, 130, "期望 '('");

        var columns = new List<ColumnDef> { parse_column_def() };
        while (match(SqlTokenType.comma))
        {
            if (is_table_constraint_start())
            {
                parse_table_constraint(columns);
                continue;
            }

            columns.Add(parse_column_def());
        }

        consume(SqlTokenType.right_paren, 131, "期望 ')'");

        return new CreateTableStatement(table, columns, ifNotExists);
    }

    private ColumnDef parse_column_def()
    {
        var name = consume_identifier(132, "期望列名");
        var type = parse_type_name();

        var isPrimaryKey = false;
        var isNotNull = false;
        var isAutoincrement = false;
        var isUnique = false;
        SqlExpression? defaultValue = null;
        SqlExpression? checkExpression = null;
        string? collate = null;

        while (true)
            if (match(SqlTokenType.primary))
            {
                consume(SqlTokenType.key, 133, "期望 'KEY'");
                isPrimaryKey = true;
                match(SqlTokenType.autoincrement);
                if (match(SqlTokenType.autoincrement)) isAutoincrement = true;
            }
            else if (check(SqlTokenType.not))
            {
                advance();
                consume(SqlTokenType.@null, 134, "期望 'NULL'");
                isNotNull = true;
            }
            else if (match(SqlTokenType.@default))
            {
                defaultValue = parse_expression();
            }
            else if (match(SqlTokenType.unique))
            {
                isUnique = true;
            }
            else if (match(SqlTokenType.check))
            {
                consume(SqlTokenType.left_paren, 144, "期望 '('");
                checkExpression = parse_expression();
                consume(SqlTokenType.right_paren, 145, "期望 ')'");
            }
            else if (match(SqlTokenType.collate))
            {
                collate = consume_identifier(146, "期望排序规则名");
            }
            else
            {
                break;
            }

        return new ColumnDef(name, type, isPrimaryKey, isNotNull, defaultValue,
            isAutoincrement, isUnique, checkExpression, collate);
    }


    /// <summary>
    ///     检查当前 token 是否是表级约束关键字
    /// </summary>
    private bool is_table_constraint_start()
    {
        return check(SqlTokenType.primary)
               || check(SqlTokenType.unique)
               || check(SqlTokenType.foreign)
               || check(SqlTokenType.constraint)
               || check(SqlTokenType.check);
    }


    /// <summary>
    ///     解析表级约束（PRIMARY KEY、UNIQUE、FOREIGN KEY、CHECK）
    /// </summary>
    private void parse_table_constraint(List<ColumnDef> columns)
    {
        if (match(SqlTokenType.constraint)) consume_identifier(136, "期望约束名");

        if (match(SqlTokenType.primary))
        {
            consume(SqlTokenType.key, 137, "期望 'KEY'");
            consume(SqlTokenType.left_paren, 138, "期望 '('");

            var pkCols = new List<string> { consume_identifier(139, "期望主键列名") };
            while (match(SqlTokenType.comma)) pkCols.Add(consume_identifier(139, "期望主键列名"));

            consume(SqlTokenType.right_paren, 140, "期望 ')'");

            for (var i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                if (pkCols.Any(pk => string.Equals(pk, col.name, StringComparison.OrdinalIgnoreCase)))
                    columns[i] = new ColumnDef(col.name, col.type, true, col.is_not_null, col.default_value);
            }
        }
        else if (match(SqlTokenType.unique))
        {
            consume(SqlTokenType.left_paren, 138, "期望 '('");
            var cols = new List<string> { consume_identifier(141, "期望唯一约束列名") };
            while (match(SqlTokenType.comma)) cols.Add(consume_identifier(141, "期望唯一约束列名"));

            consume(SqlTokenType.right_paren, 140, "期望 ')'");
        }
        else if (match(SqlTokenType.foreign))
        {
            consume(SqlTokenType.key, 137, "期望 'KEY'");
            consume(SqlTokenType.left_paren, 138, "期望 '('");
            consume_identifier(142, "期望外键列名");
            while (match(SqlTokenType.comma)) consume_identifier(142, "期望外键列名");

            consume(SqlTokenType.right_paren, 140, "期望 ')'");

            if (match(SqlTokenType.references))
            {
                consume_identifier(143, "期望引用表名");
                consume(SqlTokenType.left_paren, 138, "期望 '('");
                consume_identifier(143, "期望引用列名");
                while (match(SqlTokenType.comma)) consume_identifier(143, "期望引用列名");

                consume(SqlTokenType.right_paren, 140, "期望 ')'");
            }
        }
        else if (match(SqlTokenType.check))
        {
            consume(SqlTokenType.left_paren, 138, "期望 '('");
            var depth = 1;
            while (depth > 0 && !is_at_end())
            {
                if (check(SqlTokenType.left_paren))
                    depth++;
                else if (check(SqlTokenType.right_paren)) depth--;

                advance();
            }
        }
    }

    #endregion

    #region SHOW / DESCRIBE

    private SqlNode parse_show_tables()
    {
        consume(SqlTokenType.show, 601, "期望 'SHOW'");

        if (match(SqlTokenType.tables)) return new ShowTablesStatement();

        if (match(SqlTokenType.columns))
        {
            consume(SqlTokenType.from, 603, "期望 'FROM'");
            var tableName = consume_identifier(604, "期望表名");
            return new ShowColumnsStatement(tableName);
        }

        _diagnostics?.report_error(string.Empty, default, 605, "期望 'TABLES' 或 'COLUMNS'");
        return null;
    }

    private SqlNode parse_describe_table()
    {
        consume(SqlTokenType.describe, 610, "期望 'DESCRIBE'");
        var tableName = consume_identifier(611, "期望表名");
        return new DescribeTableStatement(tableName);
    }

    #endregion

    #region MATERIALIZED VIEW

    private SqlNode parse_create_materialized_view()
    {
        consume(SqlTokenType.create, 620, "期望 'CREATE'");
        consume(SqlTokenType.materialized, 621, "期望 'MATERIALIZED'");
        consume(SqlTokenType.view, 622, "期望 'VIEW'");

        var ifNotExists = false;
        if (match(SqlTokenType.@if))
        {
            consume(SqlTokenType.not, 623, "期望 'NOT'");
            consume(SqlTokenType.exists, 624, "期望 'EXISTS'");
            ifNotExists = true;
        }

        var name = consume_identifier(625, "期望视图名称");

        string? refreshMode = null;
        if (match(SqlTokenType.refresh))
        {
            if (match(SqlTokenType.complete))
                refreshMode = "COMPLETE";
            else if (match(SqlTokenType.fast)) refreshMode = "FAST";
        }

        consume(SqlTokenType.@as, 626, "期望 'AS'");
        var selectStmt = (SelectStatement)parse_select_or_compound();

        return new CreateMaterializedViewStatement(name, selectStmt, refreshMode, ifNotExists);
    }

    private SqlNode parse_drop_materialized_view()
    {
        consume(SqlTokenType.drop, 630, "期望 'DROP'");
        consume(SqlTokenType.materialized, 631, "期望 'MATERIALIZED'");
        consume(SqlTokenType.view, 632, "期望 'VIEW'");

        var ifExists = false;
        if (match(SqlTokenType.@if))
        {
            consume(SqlTokenType.exists, 633, "期望 'EXISTS'");
            ifExists = true;
        }

        var name = consume_identifier(634, "期望视图名称");

        return new DropMaterializedViewStatement(name, ifExists);
    }

    private SqlNode parse_refresh_materialized_view()
    {
        consume(SqlTokenType.refresh, 640, "期望 'REFRESH'");
        consume(SqlTokenType.materialized, 641, "期望 'MATERIALIZED'");
        consume(SqlTokenType.view, 642, "期望 'VIEW'");

        var name = consume_identifier(643, "期望视图名称");

        return new RefreshMaterializedViewStatement(name);
    }

    #endregion

    #region 预处理语句

    private SqlNode parse_prepare()
    {
        consume(SqlTokenType.prepare, 650, "期望 'PREPARE'");
        var name = consume_identifier(651, "期望语句名称");
        consume(SqlTokenType.from, 652, "期望 'FROM'");
        var rawText = _current.text;
        consume(SqlTokenType.@string, 653, "期望查询字符串");

        var query = rawText;

        return new PrepareStatement(name, query);
    }

    private SqlNode parse_execute()
    {
        consume(SqlTokenType.execute, 654, "期望 'EXECUTE'");
        var name = consume_identifier(655, "期望语句名称");

        var parameters = new List<SqlExpression>();
        if (match(SqlTokenType.@using))
        {
            parameters.Add(parse_expression());
            while (match(SqlTokenType.comma)) parameters.Add(parse_expression());
        }

        return new ExecuteStatement(name, parameters);
    }

    private SqlNode parse_deallocate_prepare()
    {
        if (_current.type == SqlTokenType.drop)
            consume(SqlTokenType.drop, 656, "期望 'DROP'");
        else
            consume(SqlTokenType.deallocate, 656, "期望 'DEALLOCATE'");

        consume(SqlTokenType.prepare, 657, "期望 'PREPARE'");
        var name = consume_identifier(658, "期望语句名称");

        return new DeallocatePrepareStatement(name);
    }

    #endregion

    #region 函数与存储过程

    private SqlNode parse_create_function()
    {
        consume(SqlTokenType.create, 201, "期望 'CREATE'");
        consume(SqlTokenType.function, 202, "期望 'FUNCTION'");
        var name = consume_identifier(203, "期望函数名");
        consume(SqlTokenType.left_paren, 204, "期望 '('");
        var parameters = new List<ParameterDef>();

        if (!check(SqlTokenType.right_paren))
        {
            parameters.Add(parse_parameter_def());
            while (match(SqlTokenType.comma)) parameters.Add(parse_parameter_def());
        }

        consume(SqlTokenType.right_paren, 205, "期望 ')'");
        consume(SqlTokenType.returns, 206, "期望 'RETURNS'");
        var returnType = consume_identifier(207, "期望返回值类型").ToUpperInvariant();
        consume(SqlTokenType.@as, 208, "期望 'AS'");
        var body = parse_expression();

        return new CreateFunctionStatement(name, parameters, returnType, body);
    }

    private SqlNode parse_create_procedure()
    {
        consume(SqlTokenType.create, 301, "期望 'CREATE'");
        consume(SqlTokenType.procedure, 302, "期望 'PROCEDURE'");
        var name = consume_identifier(303, "期望存储过程名");
        consume(SqlTokenType.left_paren, 304, "期望 '('");
        var parameters = new List<ParameterDef>();

        if (!check(SqlTokenType.right_paren))
        {
            parameters.Add(parse_parameter_def());
            while (match(SqlTokenType.comma)) parameters.Add(parse_parameter_def());
        }

        consume(SqlTokenType.right_paren, 305, "期望 ')'");

        var body = new List<SqlNode>();
        consume(SqlTokenType.begin, 306, "期望 'BEGIN'");
        while (!check(SqlTokenType.end) && !is_at_end())
        {
            var stmt = parse_procedure_statement();
            if (stmt is not null) body.Add(stmt);

            match(SqlTokenType.semicolon);
        }

        consume(SqlTokenType.end, 307, "期望 'END'");

        return new CreateProcedureStatement(name, parameters, body);
    }

    private SqlNode? parse_procedure_statement()
    {
        if (check(SqlTokenType.@if)) return parse_if_statement();

        return parse_statement();
    }

    private SqlNode? parse_if_statement()
    {
        consume(SqlTokenType.@if, 500, "期望 'IF'");
        var condition = parse_expression();
        consume(SqlTokenType.then, 501, "期望 'THEN'");

        var thenBody = new List<SqlNode>();
        while (!check(SqlTokenType.elsif) && !check(SqlTokenType.@else) && !check(SqlTokenType.end_if) && !is_at_end())
        {
            var stmt = parse_statement();
            if (stmt is not null) thenBody.Add(stmt);
            match(SqlTokenType.semicolon);
        }

        var elseIfClauses = new List<ElseIfClause>();
        IReadOnlyList<SqlNode>? elseBody = null;

        while (check(SqlTokenType.elsif))
        {
            advance();
            var elsifCondition = parse_expression();
            consume(SqlTokenType.then, 502, "期望 'THEN'");

            var elsifBody = new List<SqlNode>();
            while (!check(SqlTokenType.elsif) && !check(SqlTokenType.@else) && !check(SqlTokenType.end_if) &&
                   !is_at_end())
            {
                var stmt = parse_statement();
                if (stmt is not null) elsifBody.Add(stmt);
                match(SqlTokenType.semicolon);
            }

            elseIfClauses.Add(new ElseIfClause(elsifCondition, elsifBody));
        }

        if (match(SqlTokenType.@else))
        {
            var elseList = new List<SqlNode>();
            while (!check(SqlTokenType.end_if) && !is_at_end())
            {
                var stmt = parse_statement();
                if (stmt is not null) elseList.Add(stmt);
                match(SqlTokenType.semicolon);
            }

            elseBody = elseList;
        }

        consume(SqlTokenType.end_if, 503, "期望 'END IF'");

        return new IfStatement(condition, thenBody, elseIfClauses, elseBody);
    }

    private SqlNode parse_call_stmt()
    {
        consume(SqlTokenType.call, 401, "期望 'CALL'");
        var name = consume_identifier(402, "期望存储过程名");
        consume(SqlTokenType.left_paren, 403, "期望 '('");
        var args = new List<SqlExpression>();

        if (!check(SqlTokenType.right_paren))
        {
            args.Add(parse_expression());
            while (match(SqlTokenType.comma)) args.Add(parse_expression());
        }

        consume(SqlTokenType.right_paren, 404, "期望 ')'");

        return new CallStatement(name, args);
    }

    private ParameterDef parse_parameter_def()
    {
        var paramName = consume_identifier(501, "期望参数名");
        var paramType = consume_identifier(502, "期望参数类型").ToUpperInvariant();
        return new ParameterDef(paramName, paramType);
    }

    #endregion

    #region 表达式解析

    private IReadOnlyList<SqlExpression> parse_expression_list()
    {
        var expressions = new List<SqlExpression> { parse_expression() };
        while (match(SqlTokenType.comma)) expressions.Add(parse_expression());
        return expressions;
    }

    private SqlExpression parse_expression()
    {
        return parse_or();
    }

    private SqlExpression parse_or()
    {
        var left = parse_and();

        while (match(SqlTokenType.or)) left = new BinaryExpr(left, "OR", parse_and());

        return left;
    }

    private SqlExpression parse_and()
    {
        var left = parse_not();

        while (match(SqlTokenType.and)) left = new BinaryExpr(left, "AND", parse_not());

        return left;
    }

    private SqlExpression parse_not()
    {
        if (match(SqlTokenType.not)) return new UnaryExpr("NOT", parse_not());

        return parse_comparison();
    }

    private SqlExpression parse_comparison()
    {
        var left = parse_addition();

        if (check(SqlTokenType.equal))
        {
            advance();
            return new BinaryExpr(left, "=", parse_addition());
        }

        if (check(SqlTokenType.not_equal))
        {
            advance();
            return new BinaryExpr(left, "<>", parse_addition());
        }

        if (check(SqlTokenType.less_than))
        {
            advance();
            return new BinaryExpr(left, "<", parse_addition());
        }

        if (check(SqlTokenType.greater_than))
        {
            advance();
            return new BinaryExpr(left, ">", parse_addition());
        }

        if (check(SqlTokenType.less_equal))
        {
            advance();
            return new BinaryExpr(left, "<=", parse_addition());
        }

        if (check(SqlTokenType.greater_equal))
        {
            advance();
            return new BinaryExpr(left, ">=", parse_addition());
        }

        if (check(SqlTokenType.@is))
        {
            advance();
            var negated = match(SqlTokenType.not);
            consume(SqlTokenType.@null, 136, "期望 'NULL'");
            return new IsNullExpr(left, negated);
        }

        if (check(SqlTokenType.@in) || (check(SqlTokenType.not) && peek(1) == SqlTokenType.@in))
        {
            var negated = match(SqlTokenType.not);
            if (!negated)
                advance();
            else
                consume(SqlTokenType.@in, 150, "期望 'IN'");

            consume(SqlTokenType.left_paren, 137, "期望 '('");

            if (check(SqlTokenType.select))
            {
                var subquery = parse_select_raw();
                consume(SqlTokenType.right_paren, 138, "期望 ')'");
                return new InSubqueryExpr(left, subquery, negated);
            }

            var values = parse_expression_list();
            consume(SqlTokenType.right_paren, 138, "期望 ')'");
            return new InExpr(left, values, negated);
        }

        if (check(SqlTokenType.between) || (check(SqlTokenType.not) && peek(1) == SqlTokenType.between))
        {
            var negated = match(SqlTokenType.not);
            if (!negated)
                advance();
            else
                consume(SqlTokenType.between, 151, "期望 'BETWEEN'");

            var low = parse_addition();
            consume(SqlTokenType.and, 139, "期望 'AND'");
            var high = parse_addition();
            return new BetweenExpr(left, low, high, negated);
        }

        if (check(SqlTokenType.like) || check(SqlTokenType.i_like) ||
            (check(SqlTokenType.not) && peek(1) == SqlTokenType.like) ||
            (check(SqlTokenType.not) && peek(1) == SqlTokenType.i_like))
        {
            var negated = match(SqlTokenType.not);
            var isCaseInsensitive = match(SqlTokenType.i_like) || (!negated && check(SqlTokenType.i_like));
            if (!negated && !isCaseInsensitive)
                advance();
            else if (negated && !check(SqlTokenType.like) && !check(SqlTokenType.i_like))
                consume(SqlTokenType.like, 152, "期望 'LIKE' 或 'ILIKE'");
            else if (negated && check(SqlTokenType.i_like))
                consume(SqlTokenType.i_like, 153, "期望 'ILIKE'");
            else if (isCaseInsensitive) consume(SqlTokenType.i_like, 154, "期望 'ILIKE'");

            return new LikeExpr(left, parse_addition(), negated, isCaseInsensitive);
        }

        if (
            check(SqlTokenType.glob) || (check(SqlTokenType.not) && peek(1) == SqlTokenType.glob))
        {
            var negated = match(SqlTokenType.not);
            if (!negated)
                advance();
            else
                consume(SqlTokenType.glob, 209, "期望 'GLOB'");

            return new LikeExpr(left, parse_addition(), negated);
        }

        return left;
    }

    private SqlExpression parse_addition()
    {
        var left = parse_multiplication();

        while (_current.text is "+" or "-" or "||" or "|")
        {
            var op = advance().text;
            left = new BinaryExpr(left, op, parse_multiplication());
        }

        return left;
    }

    private SqlExpression parse_multiplication()
    {
        var left = parse_unary();

        while (_current.text is "*" or "/" or "%" or "&" or "<<" or ">>")
        {
            var op = advance().text;
            left = new BinaryExpr(left, op, parse_unary());
        }

        return left;
    }

    private SqlExpression parse_unary()
    {
        if (_current.text == "-")
        {
            advance();
            return new UnaryExpr("-", parse_primary());
        }

        if (_current.text == "~" || _current.type == SqlTokenType.tilde)
        {
            advance();
            return new UnaryExpr("~", parse_primary());
        }

        return parse_primary();
    }

    private SqlExpression parse_primary()
    {
        if (match(SqlTokenType.@case)) return parse_case_expr();

        if (match(SqlTokenType.cast)) return parse_cast_expr();

        if (match(SqlTokenType.exists)) return parse_exists_expr();

        if (match(SqlTokenType.@true)) return new LiteralValue("TRUE", SqlTokenType.@true);

        if (match(SqlTokenType.@false)) return new LiteralValue("FALSE", SqlTokenType.@false);

        if (match(SqlTokenType.not))
        {
            if (check(SqlTokenType.exists))
            {
                advance();
                return parse_exists_expr(true);
            }

            return new UnaryExpr("NOT", parse_primary());
        }

        if (match(SqlTokenType.left_paren))
        {
            if (check(SqlTokenType.select))
            {
                var query = parse_select_raw();
                consume(SqlTokenType.right_paren, 148, "期望 ')'");
                return new SubqueryExpr(query);
            }

            var expr = parse_expression();
            consume(SqlTokenType.right_paren, 149, "期望 ')'");

            if (check(SqlTokenType.collate))
            {
                advance();
                return new CollateExpr(expr, consume_identifier(153, "期望排序规则名"));
            }

            return expr;
        }

        if (match(SqlTokenType.star)) return new StarExpression();

        if (check(SqlTokenType.number) || check(SqlTokenType.@string) || check(SqlTokenType.@null))
        {
            var token = advance();
            return token.type switch
            {
                SqlTokenType.number => new LiteralValue(token.text, SqlTokenType.number),
                SqlTokenType.@string => new LiteralValue(token.text, SqlTokenType.@string),
                SqlTokenType.@null => new LiteralValue(null, SqlTokenType.@null),
                _ => throw new InvalidOperationException()
            };
        }

        if (check(SqlTokenType.identifier))
        {
            var text = advance().text;
            if (peek() == SqlTokenType.dot && peek(1) == SqlTokenType.star)
            {
                advance();
                advance();
                return new StarExpression(text);
            }

            if (check(SqlTokenType.dot))
            {
                advance();
                var col = advance().text;
                return new ColumnRef(col, text);
            }

            if (match(SqlTokenType.left_paren)) return parse_function_call(text);

            if (check(SqlTokenType.collate))
            {
                var colRef = new ColumnRef(text);
                advance();
                return new CollateExpr(colRef, consume_identifier(153, "期望排序规则名"));
            }

            return new ColumnRef(text);
        }

        if (is_keyword(_current.type))
        {
            var text = _current.text;
            if (peek(1) == SqlTokenType.left_paren)
            {
                advance();
                advance();
                return parse_function_call(text);
            }

            return new ColumnRef(advance().text);
        }

        _diagnostics?.report_error(string.Empty, default,
            143, $"意外的词法单元：{_current.type}");
        advance();
        return new LiteralValue(null, SqlTokenType.@null);
    }

    #endregion

    #region 辅助方法

    private bool is_at_end()
    {
        return _current.type == SqlTokenType.end_of_file;
    }

    private bool check(SqlTokenType type)
    {
        return _current.type == type;
    }

    private bool match(SqlTokenType type)
    {
        if (_current.type != type) return false;

        advance();
        return true;
    }

    private SqlToken advance()
    {
        var token = _current;
        if (_position < _tokens.Count - 1) _position++;

        return token;
    }

    private SqlToken consume(SqlTokenType type, int? errorCode, string message)
    {
        if (_current.type == type) return advance();

        _diagnostics?.report_error(string.Empty, default,
            errorCode, $"{message}，实际遇到 {_current.type}");

        return _current;
    }

    private string consume_identifier(int? errorCode, string message)
    {
        if (_current.type == SqlTokenType.identifier) return advance().text;

        if (is_keyword(_current.type)) return advance().text;

        _diagnostics?.report_error(string.Empty, default,
            errorCode, $"{message}，实际遇到 {_current.type}");

        return _current.text;
    }

    private int parse_integer_value()
    {
        if (_current.type == SqlTokenType.number && int.TryParse(advance().text, out var value)) return value;

        _diagnostics?.report_error(string.Empty, default,
            143, "期望整数值");

        return 0;
    }

    private static bool is_keyword(SqlTokenType type)
    {
        return type is >= SqlTokenType.select and <= SqlTokenType.excluded;
    }

    private int consume_number()
    {
        if (_current.type == SqlTokenType.number && int.TryParse(advance().text, out var value)) return value;

        _diagnostics?.report_error(string.Empty, default, 144, "期望数值");

        return 0;
    }

    private SelectStatement parse_select_raw()
    {
        consume(SqlTokenType.select, 190, "期望 'SELECT'");

        var distinct = match(SqlTokenType.distinct);
        var columns = parse_column_list();

        SqlTableRef? from = null;
        if (match(SqlTokenType.from)) from = parse_table_ref();

        var joins = new List<SqlTableRef>();
        while (is_join_keyword()) joins.Add(parse_join());

        SqlExpression? where = null;
        if (match(SqlTokenType.where)) where = parse_expression();

        var groupBy = new List<SqlExpression>();
        if (match(SqlTokenType.group))
        {
            consume(SqlTokenType.by, 191, "期望 'BY'");
            groupBy.Add(parse_expression());
            while (match(SqlTokenType.comma)) groupBy.Add(parse_expression());
        }

        SqlExpression? having = null;
        if (match(SqlTokenType.having)) having = parse_expression();

        var orderBy = new List<OrderByItem>();
        if (match(SqlTokenType.order))
        {
            consume(SqlTokenType.by, 192, "期望 'BY'");
            orderBy.Add(parse_order_by_item());
            while (match(SqlTokenType.comma)) orderBy.Add(parse_order_by_item());
        }

        int? limit = null;
        if (match(SqlTokenType.limit)) limit = consume_number();

        int? offset = null;
        if (match(SqlTokenType.offset)) offset = consume_number();

        return new SelectStatement(columns, from, where, joins, groupBy, having, orderBy, limit, offset, distinct);
    }

    private CaseExpr parse_case_expr()
    {
        var whenClauses = new List<(SqlExpression When, SqlExpression Then)>();

        consume(SqlTokenType.when, 193, "期望 'WHEN'");
        var when = parse_expression();
        consume(SqlTokenType.then, 194, "期望 'THEN'");
        var then = parse_expression();
        whenClauses.Add((when, then));

        while (match(SqlTokenType.when))
        {
            when = parse_expression();
            consume(SqlTokenType.then, 194, "期望 'THEN'");
            then = parse_expression();
            whenClauses.Add((when, then));
        }

        SqlExpression? elseExpr = null;
        if (match(SqlTokenType.@else)) elseExpr = parse_expression();

        consume(SqlTokenType.end, 195, "期望 'END'");

        return new CaseExpr(whenClauses, elseExpr);
    }

    private CastExpr parse_cast_expr()
    {
        consume(SqlTokenType.left_paren, 196, "期望 '('");
        var expr = parse_expression();
        consume(SqlTokenType.@as, 197, "期望 'AS'");
        var type = parse_type_name();
        consume(SqlTokenType.right_paren, 198, "期望 ')'");

        return new CastExpr(expr, type);
    }

    private ExistsExpr parse_exists_expr(bool negated = false)
    {
        if (!negated) consume(SqlTokenType.exists, 199, "期望 'EXISTS'");

        consume(SqlTokenType.left_paren, 200, "期望 '('");
        var subquery = parse_select_raw();
        consume(SqlTokenType.right_paren, 201, "期望 ')'");

        return new ExistsExpr(subquery, negated);
    }

    private FunctionCall parse_function_call(string name)
    {
        var distinct = match(SqlTokenType.distinct);
        var args = new List<SqlExpression>();
        if (!check(SqlTokenType.right_paren))
        {
            args.Add(parse_expression());
            while (match(SqlTokenType.comma)) args.Add(parse_expression());
        }

        consume(SqlTokenType.right_paren, 202, "期望 ')'");
        return new FunctionCall(name, args, distinct);
    }


    /// <summary>
    ///     解析类型名，支持带长度参数的类型如 VARCHAR(255)、DECIMAL(10,2)
    ///     。
    /// </summary>
    private string parse_type_name()
    {
        var sb = new StringBuilder();

        while (true)
        {
            if (!check(SqlTokenType.identifier) &&
                !check(SqlTokenType.integer) && !check(SqlTokenType.real) &&
                !check(SqlTokenType.text) && !check(SqlTokenType.blob) &&
                !check(SqlTokenType.varchar) && !check(SqlTokenType.boolean) &&
                !check(SqlTokenType.date) && !check(SqlTokenType.timestamp) &&
                !check(SqlTokenType.big_int) && !check(SqlTokenType.small_int) &&
                !check(SqlTokenType.tiny_int) && !check(SqlTokenType.@float) &&
                !check(SqlTokenType.@double) && !check(SqlTokenType.numeric) &&
                !check(SqlTokenType.@decimal) && !check(SqlTokenType.@char) &&
                !check(SqlTokenType.n_char) && !check(SqlTokenType.binary))
                break;

            if (sb.Length > 0) sb.Append(' ');

            sb.Append(advance().text);
        }

        if (match(SqlTokenType.left_paren))
        {
            sb.Append('(');
            while (!check(SqlTokenType.right_paren) && !is_at_end()) sb.Append(advance().text);

            consume(SqlTokenType.right_paren, 203, "期望 ')'");
            sb.Append(')');
        }

        return sb.ToString();
    }

    #endregion
}

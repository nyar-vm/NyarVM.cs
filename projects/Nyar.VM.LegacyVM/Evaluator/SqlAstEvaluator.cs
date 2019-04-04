using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Std.Data.Text.Sql;

namespace Nyar.VM.LegacyVM.Evaluator;

/// <summary>
///     SQL AST 求值器，使用 Oak.Sql 解析器将 SQL 源码解析为 AST 并在内存数据库中执行。
///     支持：SELECT（含 WHERE、ORDER BY、LIMIT）、INSERT、UPDATE、DELETE、CREATE TABLE、
///     表达式求值（算术、比较、AND/OR/NOT、聚合函数 COUNT/SUM/AVG/MIN/MAX）、
///     SHOW TABLES、SHOW COLUMNS、DESCRIBE、DROP TABLE、ALTER TABLE。
/// </summary>
public sealed class SqlAstEvaluator
{
    private readonly SqlDatabase _db;

    /// <summary>
    ///     创建 SQL AST 求值器
    /// </summary>
    public SqlAstEvaluator()
    {
        _db = new SqlDatabase();
    }

    /// <summary>
    ///     解析并执行 SQL 源码
    /// </summary>
    /// <param name="source">SQL 源码</param>
    /// <returns>执行结果</returns>
    public object evaluate(string source)
    {
        var parser = new SqlParser();
        var result = parser.parse(source);

        if (!result.success)
        {
            var errors = string.Join("\n", result.diagnostics.Select(d => d.message));
            Console.Error.WriteLine($"SQL 解析失败：{errors}");
            return $"错误: SQL 解析失败";
        }

        return _db.execute(result.value!);
    }

    #region SqlDatabase

    /// <summary>
    ///     内存 SQL 数据库，维护表数据并执行 SQL 语句
    /// </summary>
    private sealed class SqlDatabase
    {
        /// <summary>
        ///     表定义：表名 → 列定义列表
        /// </summary>
        private readonly Dictionary<string, TableSchema> _schemas = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     表数据：表名 → 行列表（每行是列名→值的字典）
        /// </summary>
        private readonly Dictionary<string, List<Dictionary<string, object?>>> _tables = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     自增计数器：表名 → 当前最大自增值
        /// </summary>
        private readonly Dictionary<string, long> _autoincrement_counters = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     执行 SQL 节点
        /// </summary>
        public object execute(SqlNode node)
        {
            return node switch
            {
                SelectStatement select => execute_select(select),
                InsertStatement insert => execute_insert(insert),
                UpdateStatement update => execute_update(update),
                DeleteStatement delete => execute_delete(delete),
                CreateTableStatement createTable => execute_create_table(createTable),
                DropTableStatement dropTable => execute_drop_table(dropTable),
                AlterTableStatement alterTable => execute_alter_table(alterTable),
                ShowTablesStatement => execute_show_tables(),
                ShowColumnsStatement showColumns => execute_show_columns(showColumns),
                DescribeTableStatement describe => execute_describe(describe),
                CompoundSelectStatement compound => execute_compound_select(compound),
                _ => $"不支持的操作: {node.GetType().Name}"
            };
        }

        #region SELECT

        private object execute_select(SelectStatement select)
        {
            if (select.from is null)
            {
                return execute_select_no_table(select);
            }

            var tableName = resolve_table_name(select.from);
            if (!_tables.TryGetValue(tableName, out var rows))
            {
                Console.Error.WriteLine($"表 '{tableName}' 不存在");
                return $"错误: 表 '{tableName}' 不存在";
            }

            var schema = _schemas[tableName];
            var aliasMap = build_alias_map(select.from, select.joins);

            var resultRows = new List<Dictionary<string, object?>>(rows);
            var resultSchema = schema;

            if (select.joins.Count > 0)
            {
                var joinResult = execute_joins(rows, schema, select.joins);
                resultRows = joinResult.rows;
                resultSchema = joinResult.schema;
            }

            if (select.where is not null)
            {
                resultRows =
                [
                    .. resultRows
                        .Where(row => to_bool(eval_expr(select.where, row, aliasMap)))
                ];
            }

            if (select.group_by.Count > 0)
            {
                return execute_group_by_select(select, resultRows, resultSchema, aliasMap);
            }

            var hasAggregate = select.columns.Any(col => contains_aggregate(col.expression));
            if (hasAggregate && select.group_by.Count == 0)
            {
                return execute_aggregate_select(select, resultRows, resultSchema, aliasMap);
            }

            var projected = resultRows.Select(row => project_columns(select.columns, row, aliasMap)).ToList();

            if (select.distinct)
            {
                projected = distinct_rows(projected);
            }

            if (select.order_by.Count > 0)
            {
                projected = execute_order_by(projected, select.order_by, resultRows, resultSchema, aliasMap);
            }

            if (select.offset.HasValue)
            {
                projected = [.. projected.Skip(select.offset.Value)];
            }

            if (select.limit.HasValue)
            {
                projected = [.. projected.Take(select.limit.Value)];
            }

            return print_result(projected);
        }

        private object execute_select_no_table(SelectStatement select)
        {
            var emptyRow = new Dictionary<string, object?>();
            var projected = project_columns(select.columns, emptyRow, new Dictionary<string, string>());
            var rows = new List<Dictionary<string, object?>> { projected };
            return print_result(rows);
        }

        private object execute_group_by_select(
            SelectStatement select,
            List<Dictionary<string, object?>> rows,
            TableSchema schema,
            Dictionary<string, string> aliasMap)
        {
            var groups = new Dictionary<string, List<Dictionary<string, object?>>>();

            foreach (var row in rows)
            {
                var key = string.Join("|", select.group_by.Select(g => to_str(eval_expr(g, row, aliasMap))));
                if (!groups.ContainsKey(key))
                {
                    groups[key] = [];
                }

                groups[key].Add(row);
            }

            var projected = new List<Dictionary<string, object?>>();

            foreach (var group in groups.Values)
            {
                var firstRow = group[0];
                var projectedRow = new Dictionary<string, object?>();

                foreach (var col in select.columns)
                {
                    var colName = get_column_name(col);
                    var value = eval_expr_with_group(col.expression, firstRow, group, aliasMap);
                    projectedRow[colName] = value;
                }

                projected.Add(projectedRow);
            }

            if (select.having is not null)
            {
                projected =
                [
                    .. projected
                        .Where(row =>
                        {
                            var groupKey = string.Join("|",
                                select.group_by.Select(g => to_str(row.GetValueOrDefault(get_expr_alias(g), null))));
                            return groups.TryGetValue(groupKey, out var group) &&
                                   to_bool(eval_expr_with_group(select.having, group[0], group, aliasMap));
                        })
                ];
            }

            return print_result(projected);
        }

        private object execute_aggregate_select(
            SelectStatement select,
            List<Dictionary<string, object?>> rows,
            TableSchema schema,
            Dictionary<string, string> aliasMap)
        {
            var projectedRow = new Dictionary<string, object?>();

            foreach (var col in select.columns)
            {
                var colName = get_column_name(col);
                var value = eval_expr_with_group(col.expression, rows.FirstOrDefault() ?? new Dictionary<string, object?>(), rows, aliasMap);
                projectedRow[colName] = value;
            }

            return print_result([projectedRow]);
        }

        private object execute_compound_select(CompoundSelectStatement compound)
        {
            var leftResult = execute_select_as_rows(compound.left);
            var rightResult = execute_select_as_rows(compound.right);

            if (leftResult is null || rightResult is null)
            {
                return "错误: 复合查询子句执行失败";
            }

            List<Dictionary<string, object?>> result;

            switch (compound.@operator)
            {
                case CompoundOperator.union:
                    result = [.. leftResult, .. rightResult];
                    break;
                case CompoundOperator.union_all:
                    result = [.. leftResult, .. rightResult];
                    break;
                case CompoundOperator.intersect:
                    result = [.. leftResult.Where(l => rightResult.Any(r => rows_equal(l, r)))];
                    break;
                case CompoundOperator.except:
                    result = [.. leftResult.Where(l => !rightResult.Any(r => rows_equal(l, r)))];
                    break;
                default:
                    result = leftResult;
                    break;
            }

            return print_result(result);
        }

        private List<Dictionary<string, object?>>? execute_select_as_rows(SelectStatement select)
        {
            if (select.from is null)
            {
                var emptyRow = new Dictionary<string, object?>();
                return [project_columns(select.columns, emptyRow, new Dictionary<string, string>())];
            }

            var tableName = resolve_table_name(select.from);
            if (!_tables.TryGetValue(tableName, out var rows))
            {
                return null;
            }

            var aliasMap = build_alias_map(select.from, select.joins);
            var filtered = select.where is not null
                ? [.. rows.Where(row => to_bool(eval_expr(select.where, row, aliasMap)))]
                : rows;

            return [.. filtered.Select(row => project_columns(select.columns, row, aliasMap))];
        }

        #endregion

        #region INSERT

        private object execute_insert(InsertStatement insert)
        {
            var tableName = insert.table;
            if (!_tables.TryGetValue(tableName, out var rows))
            {
                Console.Error.WriteLine($"表 '{tableName}' 不存在");
                return $"错误: 表 '{tableName}' 不存在";
            }

            var schema = _schemas[tableName];
            var insertedCount = 0;

            foreach (var valueRow in insert.values_rows)
            {
                var row = new Dictionary<string, object?>();

                if (insert.columns.Count > 0)
                {
                    for (var i = 0; i < insert.columns.Count && i < valueRow.Count; i++)
                    {
                        row[insert.columns[i]] = eval_expr(valueRow[i], new Dictionary<string, object?>(), new Dictionary<string, string>());
                    }
                }
                else
                {
                    for (var i = 0; i < valueRow.Count && i < schema.columns.Count; i++)
                    {
                        row[schema.columns[i].name] = eval_expr(valueRow[i], new Dictionary<string, object?>(), new Dictionary<string, string>());
                    }
                }

                foreach (var colDef in schema.columns)
                {
                    if (!row.ContainsKey(colDef.name))
                    {
                        if (colDef.is_autoincrement)
                        {
                            if (!_autoincrement_counters.ContainsKey(tableName))
                            {
                                _autoincrement_counters[tableName] = rows.Count;
                            }

                            _autoincrement_counters[tableName]++;
                            row[colDef.name] = _autoincrement_counters[tableName];
                        }
                        else if (colDef.default_value is not null)
                        {
                            row[colDef.name] = eval_expr(colDef.default_value, new Dictionary<string, object?>(), new Dictionary<string, string>());
                        }
                        else
                        {
                            row[colDef.name] = null;
                        }
                    }
                }

                rows.Add(row);
                insertedCount++;
            }

            Console.WriteLine($"已插入 {insertedCount} 行");
            return insertedCount;
        }

        #endregion

        #region UPDATE

        private object execute_update(UpdateStatement update)
        {
            var tableName = update.table;
            if (!_tables.TryGetValue(tableName, out var rows))
            {
                Console.Error.WriteLine($"表 '{tableName}' 不存在");
                return $"错误: 表 '{tableName}' 不存在";
            }

            var aliasMap = new Dictionary<string, string> { [tableName] = tableName };
            var updatedCount = 0;

            foreach (var row in rows)
            {
                if (update.where is not null && !to_bool(eval_expr(update.where, row, aliasMap)))
                {
                    continue;
                }

                foreach (var (column, value) in update.assignments)
                {
                    row[column] = eval_expr(value, row, aliasMap);
                }

                updatedCount++;
            }

            Console.WriteLine($"已更新 {updatedCount} 行");
            return updatedCount;
        }

        #endregion

        #region DELETE

        private object execute_delete(DeleteStatement delete)
        {
            var tableName = delete.table;
            if (!_tables.TryGetValue(tableName, out var rows))
            {
                Console.Error.WriteLine($"表 '{tableName}' 不存在");
                return $"错误: 表 '{tableName}' 不存在";
            }

            var aliasMap = new Dictionary<string, string> { [tableName] = tableName };
            int deletedCount;

            if (delete.where is not null)
            {
                var toDelete = rows.Where(row => to_bool(eval_expr(delete.where, row, aliasMap))).ToList();
                deletedCount = toDelete.Count;

                foreach (var row in toDelete)
                {
                    rows.Remove(row);
                }
            }
            else
            {
                deletedCount = rows.Count;
                rows.Clear();
            }

            Console.WriteLine($"已删除 {deletedCount} 行");
            return deletedCount;
        }

        #endregion

        #region DDL

        private object execute_create_table(CreateTableStatement createTable)
        {
            var tableName = createTable.table;

            if (_tables.ContainsKey(tableName))
            {
                if (createTable.if_not_exists)
                {
                    Console.WriteLine($"表 '{tableName}' 已存在（IF NOT EXISTS 跳过）");
                    return 0;
                }

                Console.Error.WriteLine($"表 '{tableName}' 已存在");
                return $"错误: 表 '{tableName}' 已存在";
            }

            _schemas[tableName] = new TableSchema([.. createTable.columns]);
            _tables[tableName] = [];
            Console.WriteLine($"已创建表 '{tableName}'");
            return 0;
        }

        private object execute_drop_table(DropTableStatement dropTable)
        {
            var tableName = dropTable.table;

            if (!_tables.ContainsKey(tableName))
            {
                if (dropTable.if_exists)
                {
                    Console.WriteLine($"表 '{tableName}' 不存在（IF EXISTS 跳过）");
                    return 0;
                }

                Console.Error.WriteLine($"表 '{tableName}' 不存在");
                return $"错误: 表 '{tableName}' 不存在";
            }

            _tables.Remove(tableName);
            _schemas.Remove(tableName);
            _autoincrement_counters.Remove(tableName);
            Console.WriteLine($"已删除表 '{tableName}'");
            return 0;
        }

        private object execute_alter_table(AlterTableStatement alterTable)
        {
            var tableName = alterTable.table;

            if (!_tables.TryGetValue(tableName, out var rows) || !_schemas.TryGetValue(tableName, out var schema))
            {
                Console.Error.WriteLine($"表 '{tableName}' 不存在");
                return $"错误: 表 '{tableName}' 不存在";
            }

            switch (alterTable.action)
            {
                case AddColumnAction addCol:
                    var newColumns = new List<ColumnDef>(schema.columns) { addCol.column };
                    _schemas[tableName] = new TableSchema(newColumns);
                    foreach (var row in rows)
                    {
                        row[addCol.column.name] = addCol.column.default_value is not null
                            ? eval_expr(addCol.column.default_value, new Dictionary<string, object?>(), new Dictionary<string, string>())
                            : null;
                    }

                    Console.WriteLine($"已添加列 '{addCol.column.name}'");
                    break;

                case DropColumnAction dropCol:
                    _schemas[tableName] = new TableSchema([
                        .. schema.columns.Where(c =>
                            !string.Equals(c.name, dropCol.column_name, StringComparison.OrdinalIgnoreCase))
                    ]);
                    foreach (var row in rows)
                    {
                        row.Remove(dropCol.column_name);
                    }

                    Console.WriteLine($"已删除列 '{dropCol.column_name}'");
                    break;

                case RenameColumnAction renameCol:
                    var renameIdx = schema.columns.FindIndex(c => string.Equals(c.name, renameCol.old_name, StringComparison.OrdinalIgnoreCase));
                    if (renameIdx >= 0)
                    {
                        var oldCol = schema.columns[renameIdx];
                        schema.columns[renameIdx] = new ColumnDef(renameCol.new_name, oldCol.type, oldCol.is_primary_key, oldCol.is_not_null, oldCol.default_value, oldCol.is_autoincrement, oldCol.is_unique, oldCol.check_expression, oldCol.collate);
                    }

                    foreach (var row in rows)
                    {
                        if (row.TryGetValue(renameCol.old_name, out var val))
                        {
                            row.Remove(renameCol.old_name);
                            row[renameCol.new_name] = val;
                        }
                    }

                    Console.WriteLine($"已重命名列 '{renameCol.old_name}' 为 '{renameCol.new_name}'");
                    break;

                case RenameTableAction renameTable:
                    _schemas[renameTable.new_name] = schema;
                    _tables[renameTable.new_name] = rows;
                    _tables.Remove(tableName);
                    _schemas.Remove(tableName);
                    Console.WriteLine($"已重命名表 '{tableName}' 为 '{renameTable.new_name}'");
                    break;

                default:
                    Console.Error.WriteLine($"不支持的 ALTER TABLE 操作: {alterTable.action.GetType().Name}");
                    return $"错误: 不支持的 ALTER TABLE 操作";
            }

            return 0;
        }

        #endregion

        #region SHOW / DESCRIBE

        private object execute_show_tables()
        {
            var rows = _tables.Keys
                .Select(name => new Dictionary<string, object?> { ["table_name"] = name })
                .ToList();

            return print_result(rows);
        }

        private object execute_show_columns(ShowColumnsStatement showColumns)
        {
            var tableName = showColumns.table_name;

            if (!_schemas.TryGetValue(tableName, out var schema))
            {
                Console.Error.WriteLine($"表 '{tableName}' 不存在");
                return $"错误: 表 '{tableName}' 不存在";
            }

            var rows = schema.columns.Select(col => new Dictionary<string, object?>
            {
                ["column_name"] = col.name,
                ["type"] = col.type,
                ["not_null"] = col.is_not_null,
                ["primary_key"] = col.is_primary_key,
                ["default_value"] = col.default_value?.ToString()
            }).ToList();

            return print_result(rows);
        }

        private object execute_describe(DescribeTableStatement describe)
        {
            var tableName = describe.table_name;

            if (!_schemas.TryGetValue(tableName, out var schema))
            {
                Console.Error.WriteLine($"表 '{tableName}' 不存在");
                return $"错误: 表 '{tableName}' 不存在";
            }

            var rows = schema.columns.Select(col => new Dictionary<string, object?>
            {
                ["column_name"] = col.name,
                ["type"] = col.type,
                ["not_null"] = col.is_not_null,
                ["primary_key"] = col.is_primary_key,
                ["default_value"] = col.default_value?.ToString()
            }).ToList();

            return print_result(rows);
        }

        #endregion

        #region JOIN

        private (List<Dictionary<string, object?>> rows, TableSchema schema) execute_joins(
            List<Dictionary<string, object?>> leftRows,
            TableSchema leftSchema,
            IReadOnlyList<SqlTableRef> joins)
        {
            var currentRows = leftRows;
            var currentSchema = leftSchema;

            foreach (var join in joins)
            {
                var rightTableName = join.name;
                if (!_tables.TryGetValue(rightTableName, out var rightRows))
                {
                    continue;
                }

                var rightSchema = _schemas[rightTableName];
                var mergedSchema = merge_schemas(currentSchema, rightSchema, rightTableName);
                var mergedRows = new List<Dictionary<string, object?>>();

                foreach (var leftRow in currentRows)
                {
                    foreach (var rightRow in rightRows)
                    {
                        var mergedRow = merge_rows(leftRow, rightRow, rightTableName);

                        if (join.on_condition is null || to_bool(eval_expr(join.on_condition, mergedRow, new Dictionary<string, string>())))
                        {
                            mergedRows.Add(mergedRow);
                        }
                    }
                }

                currentRows = mergedRows;
                currentSchema = mergedSchema;
            }

            return (currentRows, currentSchema);
        }

        #endregion

        #region 表达式求值

        private object? eval_expr(SqlExpression expr, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            return expr switch
            {
                LiteralValue lit => eval_literal(lit),
                ColumnRef colRef => eval_column_ref(colRef, row, aliasMap),
                BinaryExpr binary => eval_binary(binary, row, aliasMap),
                UnaryExpr unary => eval_unary(unary, row, aliasMap),
                FunctionCall func => eval_function(func, row, aliasMap),
                StarExpression star => eval_star(star, row),
                IsNullExpr isNull => eval_is_null(isNull, row, aliasMap),
                InExpr inExpr => eval_in(inExpr, row, aliasMap),
                BetweenExpr between => eval_between(between, row, aliasMap),
                LikeExpr like => eval_like(like, row, aliasMap),
                CaseExpr caseExpr => eval_case(caseExpr, row, aliasMap),
                CastExpr cast => eval_cast(cast, row, aliasMap),
                CollateExpr collate => eval_expr(collate.expression, row, aliasMap),
                SubqueryExpr => null,
                ExistsExpr => false,
                InSubqueryExpr => false,
                _ => null
            };
        }

        private static object? eval_literal(LiteralValue lit)
        {
            return lit.type switch
            {
                SqlTokenType.number when long.TryParse(lit.value?.ToString(), out var l) => l,
                SqlTokenType.number when double.TryParse(lit.value?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) => d,
                SqlTokenType.@string => lit.value?.ToString()?.Trim('\'', '"'),
                SqlTokenType.@true => true,
                SqlTokenType.@false => false,
                SqlTokenType.@null => null,
                _ => lit.value
            };
        }

        private static object? eval_column_ref(ColumnRef colRef, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            if (colRef.table is not null)
            {
                var resolvedTable = aliasMap.GetValueOrDefault(colRef.table, colRef.table);
                var qualifiedKey = $"{resolvedTable}.{colRef.name}";

                if (row.TryGetValue(qualifiedKey, out var val))
                {
                    return val;
                }
            }

            if (row.TryGetValue(colRef.name, out var directVal))
            {
                return directVal;
            }

            foreach (var key in row.Keys)
            {
                if (string.Equals(key, colRef.name, StringComparison.OrdinalIgnoreCase))
                {
                    return row[key];
                }

                var dotIdx = key.IndexOf('.');
                if (dotIdx >= 0 && string.Equals(key[(dotIdx + 1)..], colRef.name, StringComparison.OrdinalIgnoreCase))
                {
                    return row[key];
                }
            }

            return null;
        }

        private object? eval_binary(BinaryExpr binary, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var op = binary.@operator.ToUpperInvariant();

            if (op == "AND")
            {
                var left = eval_expr(binary.left, row, aliasMap);
                if (!to_bool(left))
                {
                    return false;
                }

                return to_bool(eval_expr(binary.right, row, aliasMap));
            }

            if (op == "OR")
            {
                var left = eval_expr(binary.left, row, aliasMap);
                if (to_bool(left))
                {
                    return true;
                }

                return to_bool(eval_expr(binary.right, row, aliasMap));
            }

            var leftVal = eval_expr(binary.left, row, aliasMap);
            var rightVal = eval_expr(binary.right, row, aliasMap);

            return op switch
            {
                "+" => to_number(leftVal) + to_number(rightVal),
                "-" => to_number(leftVal) - to_number(rightVal),
                "*" => to_number(leftVal) * to_number(rightVal),
                "/" => to_number(rightVal) != 0 ? to_number(leftVal) / to_number(rightVal) : 0.0,
                "%" => to_number(rightVal) != 0 ? to_number(leftVal) % to_number(rightVal) : 0.0,
                "=" => equals(leftVal, rightVal),
                "<>" => !equals(leftVal, rightVal),
                "<" => compare(leftVal, rightVal) < 0,
                ">" => compare(leftVal, rightVal) > 0,
                "<=" => compare(leftVal, rightVal) <= 0,
                ">=" => compare(leftVal, rightVal) >= 0,
                "||" => $"{to_str(leftVal)}{to_str(rightVal)}",
                "&" => (long)to_number(leftVal) & (long)to_number(rightVal),
                "|" => (long)to_number(leftVal) | (long)to_number(rightVal),
                "<<" => (long)to_number(leftVal) << (int)to_number(rightVal),
                ">>" => (long)to_number(leftVal) >> (int)to_number(rightVal),
                _ => null
            };
        }

        private object? eval_unary(UnaryExpr unary, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var op = unary.@operator.ToUpperInvariant();
            var val = eval_expr(unary.operand, row, aliasMap);

            return op switch
            {
                "NOT" => !to_bool(val),
                "-" => -to_number(val),
                "~" => ~(long)to_number(val),
                _ => val
            };
        }

        private object? eval_function(FunctionCall func, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var funcName = func.name.ToUpperInvariant();

            if (is_aggregate_function(funcName))
            {
                return eval_aggregate_in_row(func, row, aliasMap);
            }

            var args = func.arguments.Select(a => eval_expr(a, row, aliasMap)).ToArray();

            return funcName switch
            {
                "ABS" => Math.Abs(to_number(args.ElementAtOrDefault(0))),
                "UPPER" => to_str(args.ElementAtOrDefault(0)).ToUpperInvariant(),
                "LOWER" => to_str(args.ElementAtOrDefault(0)).ToLowerInvariant(),
                "LENGTH" or "LEN" => to_str(args.ElementAtOrDefault(0)).Length,
                "TRIM" => to_str(args.ElementAtOrDefault(0)).Trim(),
                "LTRIM" => to_str(args.ElementAtOrDefault(0)).TrimStart(),
                "RTRIM" => to_str(args.ElementAtOrDefault(0)).TrimEnd(),
                "COALESCE" => args.FirstOrDefault(a => a is not null),
                "NULLIF" => equals(args.ElementAtOrDefault(0), args.ElementAtOrDefault(1)) ? null : args.ElementAtOrDefault(0),
                "IFNULL" => args.ElementAtOrDefault(0) ?? args.ElementAtOrDefault(1),
                "TYPEOF" => args.ElementAtOrDefault(0)?.GetType().Name ?? "NULL",
                "ROUND" => Math.Round(to_number(args.ElementAtOrDefault(0)), args.Length > 1 ? (int)to_number(args.ElementAtOrDefault(1)) : 0),
                "CEIL" or "CEILING" => Math.Ceiling(to_number(args.ElementAtOrDefault(0))),
                "FLOOR" => Math.Floor(to_number(args.ElementAtOrDefault(0))),
                "SUBSTR" or "SUBSTRING" => eval_substr(args),
                "REPLACE" => to_str(args.ElementAtOrDefault(0)).Replace(to_str(args.ElementAtOrDefault(1)), to_str(args.ElementAtOrDefault(2))),
                "CONCAT" => string.Join("", args.Select(a => to_str(a))),
                _ => null
            };
        }

        private static string eval_substr(object?[] args)
        {
            var str = to_str(args.ElementAtOrDefault(0));
            var start = (int)to_number(args.ElementAtOrDefault(1)) - 1;

            if (start < 0)
            {
                start = 0;
            }

            if (args.Length > 2)
            {
                var length = (int)to_number(args.ElementAtOrDefault(2));
                return start < str.Length ? str.Substring(start, Math.Min(length, str.Length - start)) : "";
            }

            return start < str.Length ? str[start..] : "";
        }

        private object? eval_aggregate_in_row(FunctionCall func, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var funcName = func.name.ToUpperInvariant();

            if (funcName == "COUNT" && func.arguments.Count == 0)
            {
                return 1L;
            }

            if (funcName == "COUNT")
            {
                var val = eval_expr(func.arguments[0], row, aliasMap);
                return val is not null ? 1L : 0L;
            }

            return eval_expr(func.arguments.ElementAtOrDefault(0), row, aliasMap);
        }

        private object? eval_expr_with_group(SqlExpression expr, Dictionary<string, object?> row, List<Dictionary<string, object?>> group, Dictionary<string, string> aliasMap)
        {
            if (expr is FunctionCall func && is_aggregate_function(func.name.ToUpperInvariant()))
            {
                return eval_aggregate(func, group, aliasMap);
            }

            if (expr is BinaryExpr binary)
            {
                var left = eval_expr_with_group(binary.left, row, group, aliasMap);
                var right = eval_expr_with_group(binary.right, row, group, aliasMap);

                return binary.@operator.ToUpperInvariant() switch
                {
                    "+" => to_number(left) + to_number(right),
                    "-" => to_number(left) - to_number(right),
                    "*" => to_number(left) * to_number(right),
                    "/" => to_number(right) != 0 ? to_number(left) / to_number(right) : 0.0,
                    _ => null
                };
            }

            return eval_expr(expr, row, aliasMap);
        }

        private object? eval_aggregate(FunctionCall func, List<Dictionary<string, object?>> group, Dictionary<string, string> aliasMap)
        {
            var funcName = func.name.ToUpperInvariant();

            if (funcName == "COUNT")
            {
                if (func.arguments.Count == 0)
                {
                    return (long)group.Count;
                }

                return (long)group.Count(row => eval_expr(func.arguments[0], row, aliasMap) is not null);
            }

            var values = group
                .Select(row => eval_expr(func.arguments.ElementAtOrDefault(0), row, aliasMap))
                .Where(v => v is not null)
                .Select(v => to_number(v))
                .ToList();

            if (values.Count == 0)
            {
                return funcName switch
                {
                    "COUNT" => 0L,
                    _ => null
                };
            }

            return funcName switch
            {
                "SUM" => values.Sum(),
                "AVG" => values.Average(),
                "MIN" => values.Min(),
                "MAX" => values.Max(),
                _ => null
            };
        }

        private object eval_is_null(IsNullExpr isNull, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var val = eval_expr(isNull.expression, row, aliasMap);
            var result = val is null;
            return isNull.negated ? !result : result;
        }

        private object eval_in(InExpr inExpr, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var val = eval_expr(inExpr.expression, row, aliasMap);
            var found = inExpr.values.Any(v => equals(val, eval_expr(v, row, aliasMap)));
            return inExpr.negated ? !found : found;
        }

        private object eval_between(BetweenExpr between, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var val = eval_expr(between.expression, row, aliasMap);
            var low = eval_expr(between.low, row, aliasMap);
            var high = eval_expr(between.high, row, aliasMap);
            var result = compare(val, low) >= 0 && compare(val, high) <= 0;
            return between.negated ? !result : result;
        }

        private object eval_like(LikeExpr like, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var val = to_str(eval_expr(like.expression, row, aliasMap));
            var pattern = to_str(eval_expr(like.pattern, row, aliasMap));
            var regex = like_to_regex(pattern);
            var result = Regex.IsMatch(val, regex, like.case_insensitive ? RegexOptions.IgnoreCase : RegexOptions.None);
            return like.negated ? !result : result;
        }

        private object? eval_case(CaseExpr caseExpr, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            foreach (var (when, then) in caseExpr.when_clauses)
            {
                if (to_bool(eval_expr(when, row, aliasMap)))
                {
                    return eval_expr(then, row, aliasMap);
                }
            }

            return caseExpr.else_expr is not null ? eval_expr(caseExpr.else_expr, row, aliasMap) : null;
        }

        private object? eval_cast(CastExpr cast, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var val = eval_expr(cast.expression, row, aliasMap);
            var targetType = cast.target_type.ToUpperInvariant();

            return targetType switch
            {
                "INTEGER" or "BIGINT" or "INT" or "SMALLINT" or "TINYINT" => (long)to_number(val),
                "REAL" or "FLOAT" or "DOUBLE" or "DOUBLE PRECISION" or "NUMERIC" or "DECIMAL" => to_number(val),
                "TEXT" or "VARCHAR" or "CHAR" or "CHARACTER" => to_str(val),
                "BOOLEAN" or "BOOL" => to_bool(val),
                _ => val
            };
        }

        private static object? eval_star(StarExpression star, Dictionary<string, object?> row)
        {
            if (star.table is not null)
            {
                var prefix = $"{star.table}.";
                var matching = row.Where(kv => kv.Key.StartsWith(prefix)).ToDictionary(kv => kv.Key, kv => kv.Value);
                return matching.Count > 0 ? matching : null;
            }

            return new Dictionary<string, object?>(row);
        }

        #endregion

        #region 辅助方法

        private static string resolve_table_name(SqlTableRef tableRef)
        {
            return tableRef.name;
        }

        private static Dictionary<string, string> build_alias_map(SqlTableRef from, IReadOnlyList<SqlTableRef> joins)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (from.alias is not null)
            {
                map[from.alias] = from.name;
            }

            foreach (var join in joins)
            {
                if (join.alias is not null)
                {
                    map[join.alias] = join.name;
                }
            }

            return map;
        }

        private Dictionary<string, object?> project_columns(IReadOnlyList<SqlColumn> columns, Dictionary<string, object?> row, Dictionary<string, string> aliasMap)
        {
            var result = new Dictionary<string, object?>();

            foreach (var col in columns)
            {
                var colName = get_column_name(col);

                if (col.expression is StarExpression)
                {
                    foreach (var kv in row)
                    {
                        var displayName = kv.Key.Contains('.') ? kv.Key[(kv.Key.LastIndexOf('.') + 1)..] : kv.Key;
                        result[displayName] = kv.Value;
                    }
                }
                else
                {
                    result[colName] = eval_expr(col.expression, row, aliasMap);
                }
            }

            return result;
        }

        private static string get_column_name(SqlColumn col)
        {
            if (col.alias is not null)
            {
                return col.alias;
            }

            return col.expression switch
            {
                ColumnRef cr => cr.table is not null ? $"{cr.table}.{cr.name}" : cr.name,
                FunctionCall fc => $"{fc.name}({string.Join(", ", fc.arguments)})",
                StarExpression => "*",
                _ => col.expression.ToString() ?? "?"
            };
        }

        private static string get_expr_alias(SqlExpression expr)
        {
            return expr switch
            {
                ColumnRef cr => cr.name,
                _ => expr.ToString() ?? "?"
            };
        }

        private static List<Dictionary<string, object?>> execute_order_by(
            List<Dictionary<string, object?>> rows,
            IReadOnlyList<OrderByItem> orderByItems,
            List<Dictionary<string, object?>> originalRows,
            TableSchema schema,
            Dictionary<string, string> aliasMap)
        {
            var sorted = rows.ToList();

            for (var i = orderByItems.Count - 1; i >= 0; i--)
            {
                var item = orderByItems[i];
                var colName = get_expr_alias(item.expression);

                sorted.Sort((a, b) =>
                {
                    var aVal = a.GetValueOrDefault(colName);
                    var bVal = b.GetValueOrDefault(colName);
                    var cmp = compare(aVal, bVal);
                    return item.descending ? -cmp : cmp;
                });
            }

            return sorted;
        }

        private static List<Dictionary<string, object?>> distinct_rows(List<Dictionary<string, object?>> rows)
        {
            var seen = new HashSet<string>();
            var result = new List<Dictionary<string, object?>>();

            foreach (var row in rows)
            {
                var key = string.Join("|", row.Values.Select(v => to_str(v)));
                if (seen.Add(key))
                {
                    result.Add(row);
                }
            }

            return result;
        }

        private static Dictionary<string, object?> merge_rows(Dictionary<string, object?> left, Dictionary<string, object?> right, string rightTableName)
        {
            var merged = new Dictionary<string, object?>();

            foreach (var kv in left)
            {
                merged[kv.Key] = kv.Value;
            }

            foreach (var kv in right)
            {
                merged[$"{rightTableName}.{kv.Key}"] = kv.Value;
                if (!merged.ContainsKey(kv.Key))
                {
                    merged[kv.Key] = kv.Value;
                }
            }

            return merged;
        }

        private static TableSchema merge_schemas(TableSchema left, TableSchema right, string rightTableName)
        {
            var columns = new List<ColumnDef>(left.columns);

            foreach (var col in right.columns)
            {
                columns.Add(new ColumnDef($"{rightTableName}.{col.name}", col.type, col.is_primary_key, col.is_not_null, col.default_value));
            }

            return new TableSchema(columns);
        }

        private static bool rows_equal(Dictionary<string, object?> a, Dictionary<string, object?> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var bVal) || !equals(kv.Value, bVal))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool contains_aggregate(SqlExpression expr)
        {
            return expr switch
            {
                FunctionCall fc => is_aggregate_function(fc.name.ToUpperInvariant()),
                BinaryExpr binary => contains_aggregate(binary.left) || contains_aggregate(binary.right),
                UnaryExpr unary => contains_aggregate(unary.operand),
                _ => false
            };
        }

        private static bool is_aggregate_function(string funcName)
        {
            return funcName is "COUNT" or "SUM" or "AVG" or "MIN" or "MAX" or "GROUP_CONCAT" or "TOTAL";
        }

        private static string like_to_regex(string pattern)
        {
            var regex = new StringBuilder();
            regex.Append('^');

            foreach (var c in pattern)
            {
                if (c == '%')
                {
                    regex.Append(".*");
                }
                else if (c == '_')
                {
                    regex.Append('.');
                }
                else if (".^$*+?{}[]|()\\".Contains(c))
                {
                    regex.Append('\\');
                    regex.Append(c);
                }
                else
                {
                    regex.Append(c);
                }
            }

            regex.Append('$');
            return regex.ToString();
        }

        private static double to_number(object? val)
        {
            return val switch
            {
                long l => l,
                int i => i,
                double d => d,
                float f => f,
                bool b => b ? 1.0 : 0.0,
                string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var r) => r,
                _ => 0.0
            };
        }

        private static bool to_bool(object? val)
        {
            return val switch
            {
                bool b => b,
                long l => l != 0,
                int i => i != 0,
                double d => d != 0,
                null => false,
                string s => s.Length > 0,
                _ => true
            };
        }

        private static string to_str(object? val)
        {
            return val switch
            {
                null => "NULL",
                bool b => b ? "1" : "0",
                double d => d % 1 == 0 ? ((long)d).ToString() : d.ToString(CultureInfo.InvariantCulture),
                _ => val.ToString() ?? "NULL"
            };
        }

        private static bool equals(object? a, object? b)
        {
            if (a is null && b is null)
            {
                return true;
            }

            if (a is null || b is null)
            {
                return false;
            }

            if (a.GetType() == b.GetType())
            {
                return a.Equals(b);
            }

            return Math.Abs(to_number(a) - to_number(b)) < 1e-10;
        }

        private static int compare(object? a, object? b)
        {
            if (a is null && b is null)
            {
                return 0;
            }

            if (a is null)
            {
                return -1;
            }

            if (b is null)
            {
                return 1;
            }

            var an = to_number(a);
            var bn = to_number(b);

            if (Math.Abs(an - bn) < 1e-10)
            {
                return 0;
            }

            return an.CompareTo(bn);
        }

        private static string print_result(List<Dictionary<string, object?>> rows)
        {
            if (rows.Count == 0)
            {
                Console.WriteLine("(0 行)");
                return "(0 行)";
            }

            var columns = rows[0].Keys.ToList();
            var colWidths = new Dictionary<string, int>();

            foreach (var col in columns)
            {
                colWidths[col] = Math.Max(col.Length, rows.Max(r => to_str(r.GetValueOrDefault(col)).Length));
            }

            var sb = new StringBuilder();

            sb.Append("| ");
            for (var i = 0; i < columns.Count; i++)
            {
                sb.Append(columns[i].PadRight(colWidths[columns[i]]));
                if (i < columns.Count - 1)
                {
                    sb.Append(" | ");
                }
            }

            sb.AppendLine(" |");

            sb.Append("|-");
            for (var i = 0; i < columns.Count; i++)
            {
                sb.Append(new string('-', colWidths[columns[i]]));
                if (i < columns.Count - 1)
                {
                    sb.Append("-+-");
                }
            }

            sb.AppendLine("-|");

            foreach (var row in rows)
            {
                sb.Append("| ");
                for (var i = 0; i < columns.Count; i++)
                {
                    var val = to_str(row.GetValueOrDefault(columns[i]));
                    sb.Append(val.PadRight(colWidths[columns[i]]));
                    if (i < columns.Count - 1)
                    {
                        sb.Append(" | ");
                    }
                }

                sb.AppendLine(" |");
            }

            sb.AppendLine($"({rows.Count} 行)");

            var output = sb.ToString();
            Console.Write(output);
            return output.Trim();
        }

        #endregion
    }

    #endregion

    #region TableSchema

    /// <summary>
    ///     表结构定义
    /// </summary>
    private sealed class TableSchema
    {
        /// <summary>
        ///     列定义列表
        /// </summary>
        public List<ColumnDef> columns { get; }

        /// <summary>
        ///     创建表结构
        /// </summary>
        /// <param name="columns">列定义列表</param>
        public TableSchema(List<ColumnDef> columns)
        {
            this.columns = columns;
        }
    }

    #endregion
}
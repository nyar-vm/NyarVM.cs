using System.Text;
using Hermes.YYDB.Query;

namespace Hermes.Database.Sqlite;

/// <summary>
///     SQLite 查询翻译器，将 <see cref="QueryExpression" /> 翻译为 SQLite 兼容的 SQL 语句。
///     支持 SQLite 特有语法：INSERT OR REPLACE、窗口函数、CTE、LIKE 全文搜索。
/// </summary>
public sealed class SqliteQueryTranslator
{
    /// <summary>
    ///     将查询表达式翻译为 SQLite SQL 语句
    /// </summary>
    public string Translate(QueryExpression query)
    {
        var sb = new StringBuilder();

        switch (query)
        {
            case CteQuery cte:
                TranslateCte(cte, sb);
                break;
            case WindowQuery window:
                TranslateWindow(window, sb);
                break;
            case FullTextSearchQuery fts:
                TranslateFullTextSearch(fts, sb);
                break;
            case UpsertQuery upsert:
                TranslateUpsert(upsert, sb);
                break;
            case FindQuery find:
                TranslateFind(find, sb);
                break;
            case CreateQuery create:
                TranslateCreate(create, sb);
                break;
            case UpdateQuery update:
                TranslateUpdate(update, sb);
                break;
            case DeleteQuery delete:
                TranslateDelete(delete, sb);
                break;
            case AggregateQuery aggregate:
                TranslateAggregate(aggregate, sb);
                break;
            default:
                throw new NotSupportedException($"不支持的查询类型：{query.QueryKind}");
        }

        return sb.ToString();
    }

    #region CTE 翻译

    /// <summary>
    ///     翻译 CTE 查询（WITH ... AS ...）
    /// </summary>
    private void TranslateCte(CteQuery query, StringBuilder sb)
    {
        var hasRecursive = query.CteDefinitions.Any(c => c.IsRecursive);

        if (hasRecursive)
            sb.Append("WITH RECURSIVE ");
        else
            sb.Append("WITH ");

        var cteItems = new List<string>();
        foreach (var cte in query.CteDefinitions)
        {
            var cteSql = new StringBuilder();
            cteSql.Append(QuoteIdentifier(cte.Name));

            if (cte.ColumnNames.Count > 0)
            {
                cteSql.Append(" (");
                cteSql.Append(string.Join(", ", cte.ColumnNames.Select(QuoteIdentifier)));
                cteSql.Append(")");
            }

            cteSql.Append(" AS (");
            cteSql.Append(Translate(cte.SubQuery));
            cteSql.Append(")");

            cteItems.Add(cteSql.ToString());
        }

        sb.Append(string.Join(", ", cteItems));
        sb.Append(" ");
        sb.Append(Translate(query.MainQuery));
    }

    #endregion

    #region 全文搜索翻译

    /// <summary>
    ///     翻译全文搜索查询（SQLite 使用 LIKE 或 FTS5 虚拟表）
    /// </summary>
    private void TranslateFullTextSearch(FullTextSearchQuery query, StringBuilder sb)
    {
        sb.Append("SELECT * FROM ");
        sb.Append(QuoteIdentifier(query.TargetTypeName));

        var conditions = new List<string>();
        foreach (var field in query.SearchFields)
            conditions.Add($"{QuoteIdentifier(field)} LIKE '%{EscapeString(query.SearchTerm)}%'");

        sb.Append(" WHERE (");
        sb.Append(string.Join(" OR ", conditions));
        sb.Append(")");

        if (query.Predicate != null)
        {
            sb.Append(" AND ");
            TranslatePredicate(query.Predicate, sb);
        }

        if (query.Ordering != null)
        {
            sb.Append(" ORDER BY ");
            sb.Append(QuoteIdentifier(query.Ordering.FieldName));
            if (query.Ordering.Descending) sb.Append(" DESC");
        }

        if (query.Pagination != null)
        {
            sb.Append($" LIMIT {query.Pagination.Limit}");
            if (query.Pagination.Offset > 0) sb.Append($" OFFSET {query.Pagination.Offset}");
        }
    }

    #endregion

    #region UPSERT 翻译

    /// <summary>
    ///     翻译 UPSERT 查询（SQLite 使用 INSERT OR REPLACE）
    /// </summary>
    private void TranslateUpsert(UpsertQuery query, StringBuilder sb)
    {
        if (query.Strategy == UpsertStrategy.DoNothing)
            sb.Append("INSERT OR IGNORE INTO ");
        else
            sb.Append("INSERT OR REPLACE INTO ");

        sb.Append(QuoteIdentifier(query.TargetTypeName));
        sb.Append(" (");

        var fields = query.InsertAssignments.Select(a => QuoteIdentifier(a.FieldName)).ToList();
        sb.Append(string.Join(", ", fields));

        sb.Append(") VALUES (");

        var values = query.InsertAssignments.Select(a => FormatValue(a.Value)).ToList();
        sb.Append(string.Join(", ", values));

        sb.Append(")");
    }

    #endregion

    #region 谓词翻译

    private void TranslatePredicate(QueryPredicate predicate, StringBuilder sb)
    {
        switch (predicate)
        {
            case FieldEquals eq:
                sb.Append($"{QuoteIdentifier(eq.FieldName)} = {FormatValue(eq.Value)}");
                break;
            case FieldGreaterThan gt:
                sb.Append($"{QuoteIdentifier(gt.FieldName)} > {FormatValue(gt.Value)}");
                break;
            case FieldLessThan lt:
                sb.Append($"{QuoteIdentifier(lt.FieldName)} < {FormatValue(lt.Value)}");
                break;
            case FieldContains ct:
                sb.Append($"{QuoteIdentifier(ct.FieldName)} LIKE '%{EscapeString(ct.Value?.ToString() ?? "")}%'");
                break;
            case FieldIn fin:
                var values = string.Join(", ", fin.Values.Select(FormatValue));
                sb.Append($"{QuoteIdentifier(fin.FieldName)} IN ({values})");
                break;
            case AndPredicate and:
                sb.Append("(");
                TranslatePredicate(and.Left, sb);
                sb.Append(" AND ");
                TranslatePredicate(and.Right, sb);
                sb.Append(")");
                break;
            case OrPredicate or:
                sb.Append("(");
                TranslatePredicate(or.Left, sb);
                sb.Append(" OR ");
                TranslatePredicate(or.Right, sb);
                sb.Append(")");
                break;
            case NotPredicate not:
                sb.Append("NOT (");
                TranslatePredicate(not.Inner, sb);
                sb.Append(")");
                break;
            default:
                sb.Append("1=1");
                break;
        }
    }

    #endregion

    #region 窗口函数翻译

    /// <summary>
    ///     翻译窗口函数查询（SQLite 3.25+ 支持窗口函数）
    /// </summary>
    private void TranslateWindow(WindowQuery query, StringBuilder sb)
    {
        var selectItems = new List<string> { "*" };

        foreach (var wf in query.WindowFunctions) selectItems.Add(TranslateWindowFunction(wf));

        sb.Append("SELECT ");
        sb.Append(string.Join(", ", selectItems));
        sb.Append(" FROM ");
        sb.Append(QuoteIdentifier(query.TargetTypeName));

        if (query.Predicate != null)
        {
            sb.Append(" WHERE ");
            TranslatePredicate(query.Predicate, sb);
        }

        if (query.Ordering != null)
        {
            sb.Append(" ORDER BY ");
            sb.Append(QuoteIdentifier(query.Ordering.FieldName));
            if (query.Ordering.Descending) sb.Append(" DESC");
        }

        if (query.Pagination != null)
        {
            sb.Append($" LIMIT {query.Pagination.Limit}");
            if (query.Pagination.Offset > 0) sb.Append($" OFFSET {query.Pagination.Offset}");
        }
    }

    /// <summary>
    ///     翻译单个窗口函数
    /// </summary>
    private string TranslateWindowFunction(WindowFunctionDefinition wf)
    {
        var funcExpr = wf.Kind switch
        {
            WindowFunctionKind.RowNumber => "ROW_NUMBER()",
            WindowFunctionKind.Rank => "RANK()",
            WindowFunctionKind.DenseRank => "DENSE_RANK()",
            WindowFunctionKind.PercentRank => "PERCENT_RANK()",
            WindowFunctionKind.NTile => $"NTILE({wf.Offset})",
            WindowFunctionKind.Lag => $"LAG({QuoteIdentifier(wf.FieldName)}, {wf.Offset})",
            WindowFunctionKind.Lead => $"LEAD({QuoteIdentifier(wf.FieldName)}, {wf.Offset})",
            WindowFunctionKind.FirstValue => $"FIRST_VALUE({QuoteIdentifier(wf.FieldName)})",
            WindowFunctionKind.LastValue => $"LAST_VALUE({QuoteIdentifier(wf.FieldName)})",
            WindowFunctionKind.NthValue => $"NTH_VALUE({QuoteIdentifier(wf.FieldName)}, {wf.Offset})",
            WindowFunctionKind.Sum => $"SUM({QuoteIdentifier(wf.FieldName)})",
            WindowFunctionKind.Avg => $"AVG({QuoteIdentifier(wf.FieldName)})",
            WindowFunctionKind.Count => $"COUNT({QuoteIdentifier(wf.FieldName)})",
            WindowFunctionKind.Min => $"MIN({QuoteIdentifier(wf.FieldName)})",
            WindowFunctionKind.Max => $"MAX({QuoteIdentifier(wf.FieldName)})",
            _ => throw new NotSupportedException($"不支持的窗口函数类型：{wf.Kind}")
        };

        var overClause = new StringBuilder();
        overClause.Append("OVER (");

        if (wf.PartitionBy.Count > 0)
        {
            overClause.Append("PARTITION BY ");
            overClause.Append(string.Join(", ", wf.PartitionBy.Select(QuoteIdentifier)));
        }

        if (wf.OrderBy.Count > 0)
        {
            if (wf.PartitionBy.Count > 0) overClause.Append(" ");

            overClause.Append("ORDER BY ");
            var orderItems = wf.OrderBy.Select(o =>
                o.Descending ? $"{QuoteIdentifier(o.FieldName)} DESC" : QuoteIdentifier(o.FieldName));
            overClause.Append(string.Join(", ", orderItems));
        }

        if (wf.Frame != null)
        {
            if (wf.OrderBy.Count > 0) overClause.Append(" ");

            overClause.Append(TranslateWindowFrame(wf.Frame));
        }

        overClause.Append(")");

        return $"{funcExpr} {overClause} AS {QuoteIdentifier(wf.Alias)}";
    }

    /// <summary>
    ///     翻译窗口帧
    /// </summary>
    private static string TranslateWindowFrame(WindowFrame frame)
    {
        var kindStr = frame.Kind switch
        {
            WindowFrameKind.Rows => "ROWS",
            WindowFrameKind.Range => "RANGE",
            WindowFrameKind.Groups => "GROUPS",
            _ => "ROWS"
        };

        return $"{kindStr} BETWEEN {TranslateFrameBound(frame.Start)} AND {TranslateFrameBound(frame.End)}";
    }

    /// <summary>
    ///     翻译帧边界
    /// </summary>
    private static string TranslateFrameBound(WindowFrameBound bound)
    {
        return bound.Kind switch
        {
            WindowFrameBoundKind.UnboundedPreceding => "UNBOUNDED PRECEDING",
            WindowFrameBoundKind.UnboundedFollowing => "UNBOUNDED FOLLOWING",
            WindowFrameBoundKind.CurrentRow => "CURRENT ROW",
            WindowFrameBoundKind.OffsetPreceding => $"{bound.OffsetRows} PRECEDING",
            WindowFrameBoundKind.OffsetFollowing => $"{bound.OffsetRows} FOLLOWING",
            _ => "CURRENT ROW"
        };
    }

    #endregion

    #region 基础 CRUD 翻译

    private void TranslateFind(FindQuery query, StringBuilder sb)
    {
        sb.Append("SELECT * FROM ");
        sb.Append(QuoteIdentifier(query.TargetTypeName));

        if (query.Predicate != null)
        {
            sb.Append(" WHERE ");
            TranslatePredicate(query.Predicate, sb);
        }

        if (query.Ordering != null)
        {
            sb.Append(" ORDER BY ");
            sb.Append(QuoteIdentifier(query.Ordering.FieldName));
            if (query.Ordering.Descending) sb.Append(" DESC");
        }

        if (query.Pagination != null)
        {
            sb.Append($" LIMIT {query.Pagination.Limit}");
            if (query.Pagination.Offset > 0) sb.Append($" OFFSET {query.Pagination.Offset}");
        }
    }

    private void TranslateCreate(CreateQuery query, StringBuilder sb)
    {
        sb.Append("INSERT INTO ");
        sb.Append(QuoteIdentifier(query.TargetTypeName));
        sb.Append(" (");

        var fields = query.Assignments.Select(a => QuoteIdentifier(a.FieldName)).ToList();
        sb.Append(string.Join(", ", fields));

        sb.Append(") VALUES (");

        var values = query.Assignments.Select(a => FormatValue(a.Value)).ToList();
        sb.Append(string.Join(", ", values));

        sb.Append(")");
    }

    private void TranslateUpdate(UpdateQuery query, StringBuilder sb)
    {
        sb.Append("UPDATE ");
        sb.Append(QuoteIdentifier(query.TargetTypeName));
        sb.Append(" SET ");

        var sets = query.Assignments.Select(a => $"{QuoteIdentifier(a.FieldName)} = {FormatValue(a.Value)}").ToList();
        sb.Append(string.Join(", ", sets));

        if (query.Predicate != null)
        {
            sb.Append(" WHERE ");
            TranslatePredicate(query.Predicate, sb);
        }
    }

    private void TranslateDelete(DeleteQuery query, StringBuilder sb)
    {
        sb.Append("DELETE FROM ");
        sb.Append(QuoteIdentifier(query.TargetTypeName));

        if (query.Predicate != null)
        {
            sb.Append(" WHERE ");
            TranslatePredicate(query.Predicate, sb);
        }
    }

    private void TranslateAggregate(AggregateQuery query, StringBuilder sb)
    {
        var selectItems = new List<string>();

        foreach (var op in query.Operations)
        {
            var alias = op.Alias != null ? $" AS {QuoteIdentifier(op.Alias)}" : "";

            if (op.Function == AggregateFunction.Distinct)
            {
                selectItems.Add($"COUNT(DISTINCT {QuoteIdentifier(op.FieldName)}){alias}");
            }
            else
            {
                var funcName = op.Function switch
                {
                    AggregateFunction.Count => "COUNT",
                    AggregateFunction.Sum => "SUM",
                    AggregateFunction.Avg => "AVG",
                    AggregateFunction.Min => "MIN",
                    AggregateFunction.Max => "MAX",
                    _ => op.Function.ToString().ToUpperInvariant()
                };
                selectItems.Add($"{funcName}({QuoteIdentifier(op.FieldName)}){alias}");
            }
        }

        if (query.GroupBy.Count > 0)
        {
            var groupByFields = query.GroupBy.Select(QuoteIdentifier).ToList();
            selectItems.AddRange(groupByFields);
        }

        sb.Append("SELECT ");
        sb.Append(string.Join(", ", selectItems));
        sb.Append(" FROM ");
        sb.Append(QuoteIdentifier(query.TargetTypeName));

        if (query.Predicate != null)
        {
            sb.Append(" WHERE ");
            TranslatePredicate(query.Predicate, sb);
        }

        if (query.GroupBy.Count > 0)
        {
            sb.Append(" GROUP BY ");
            sb.Append(string.Join(", ", query.GroupBy.Select(QuoteIdentifier)));
        }
    }

    #endregion

    #region 辅助方法

    private static string QuoteIdentifier(string identifier)
    {
        return $"\"{identifier}\"";
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "NULL",
            string s => $"'{EscapeString(s)}'",
            bool b => b ? "1" : "0",
            DateTime dt => $"'{dt:yyyy-MM-dd HH:mm:ss}'",
            DateTimeOffset dto => $"'{dto:yyyy-MM-dd HH:mm:ss}'",
            byte[] bytes => $"x'{Convert.ToHexString(bytes)}'",
            _ => value.ToString() ?? "NULL"
        };
    }

    private static string EscapeString(string s)
    {
        return s.Replace("'", "''");
    }

    #endregion
}
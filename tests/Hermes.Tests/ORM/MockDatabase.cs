namespace Hermes.ORM.Tests;

/// <summary>
///     内存模拟数据库，用于 ORM CRUD 测试
/// </summary>
public sealed class MockDatabase
{
    private readonly Dictionary<string, List<Dictionary<string, object?>>> _tables = new();

    public IReadOnlyList<Dictionary<string, object?>> GetTable(string tableName)
    {
        if (!_tables.TryGetValue(tableName, out var rows)) return [];

        return rows;
    }

    public Func<QueryExpression, Task<OrmQueryResult>> CreateExecutor()
    {
        return async query =>
        {
            await Task.Yield();
            return Execute(query);
        };
    }

    private OrmQueryResult Execute(QueryExpression query)
    {
        return query switch
        {
            CreateQuery c => ExecuteCreate(c),
            FindQuery f => ExecuteFind(f),
            UpdateQuery u => ExecuteUpdate(u),
            DeleteQuery d => ExecuteDelete(d),
            AggregateQuery a => ExecuteAggregate(a),
            UpsertQuery up => ExecuteUpsert(up),
            _ => OrmQueryResult.Fail($"不支持的查询类型：{query.QueryKind}")
        };
    }

    private OrmQueryResult ExecuteCreate(CreateQuery query)
    {
        if (!_tables.TryGetValue(query.TargetTypeName, out var rows))
        {
            rows = [];
            _tables[query.TargetTypeName] = rows;
        }

        var row = new Dictionary<string, object?>();
        foreach (var assignment in query.Assignments) row[assignment.FieldName] = assignment.Value;

        rows.Add(row);

        return OrmQueryResult.Ok(1);
    }

    private OrmQueryResult ExecuteFind(FindQuery query)
    {
        if (!_tables.TryGetValue(query.TargetTypeName, out var rows)) return OrmQueryResult.Ok([]);

        var filtered = rows.AsEnumerable();

        if (query.Predicate != null) filtered = filtered.Where(r => EvaluatePredicate(r, query.Predicate));

        var resultRows = filtered.Select(r => new Dictionary<string, object?>(r)).ToList();

        if (query.Ordering != null)
        {
            var fieldName = query.Ordering.FieldName;
            resultRows = query.Ordering.Descending
                ? [.. resultRows.OrderByDescending(r => r.GetValueOrDefault(fieldName), new ObjectComparer())]
                : [.. resultRows.OrderBy(r => r.GetValueOrDefault(fieldName), new ObjectComparer())];
        }

        if (query.Pagination != null)
            resultRows =
            [
                .. resultRows
                    .Skip(query.Pagination.Offset)
                    .Take(query.Pagination.Limit)
            ];

        return OrmQueryResult.Ok(resultRows.Cast<IReadOnlyDictionary<string, object?>>().ToList());
    }

    private OrmQueryResult ExecuteUpdate(UpdateQuery query)
    {
        if (!_tables.TryGetValue(query.TargetTypeName, out var rows)) return OrmQueryResult.Ok(0);

        var affected = 0;
        foreach (var row in rows)
            if (EvaluatePredicate(row, query.Predicate))
            {
                foreach (var assignment in query.Assignments) row[assignment.FieldName] = assignment.Value;

                affected++;
            }

        return OrmQueryResult.Ok(affected);
    }

    private OrmQueryResult ExecuteDelete(DeleteQuery query)
    {
        if (!_tables.TryGetValue(query.TargetTypeName, out var rows)) return OrmQueryResult.Ok(0);

        var removed = rows.RemoveAll(r => EvaluatePredicate(r, query.Predicate));

        return OrmQueryResult.Ok(removed);
    }

    private OrmQueryResult ExecuteAggregate(AggregateQuery query)
    {
        if (!_tables.TryGetValue(query.TargetTypeName, out var rows)) return OrmQueryResult.Ok([]);

        var filtered = rows.AsEnumerable();

        if (query.Predicate != null) filtered = filtered.Where(r => EvaluatePredicate(r, query.Predicate));

        var resultList = filtered.ToList();
        var resultRow = new Dictionary<string, object?>();

        foreach (var agg in query.Operations)
        {
            var alias = agg.Alias ?? agg.FieldName;
            resultRow[alias] = agg.Function switch
            {
                AggregateFunction.Count => resultList.Count,
                AggregateFunction.Sum => resultList.Sum(r => Convert.ToDouble(r.GetValueOrDefault(agg.FieldName))),
                AggregateFunction.Avg => resultList.Average(r => Convert.ToDouble(r.GetValueOrDefault(agg.FieldName))),
                AggregateFunction.Min => resultList.Min(r => Convert.ToDouble(r.GetValueOrDefault(agg.FieldName))),
                AggregateFunction.Max => resultList.Max(r => Convert.ToDouble(r.GetValueOrDefault(agg.FieldName))),
                _ => resultList.Count
            };
        }

        return OrmQueryResult.Ok([resultRow]);
    }

    private OrmQueryResult ExecuteUpsert(UpsertQuery query)
    {
        if (!_tables.TryGetValue(query.TargetTypeName, out var rows))
        {
            rows = [];
            _tables[query.TargetTypeName] = rows;
        }

        var conflictValues = query.InsertAssignments
            .Where(a => query.ConflictFields.Contains(a.FieldName))
            .ToDictionary(a => a.FieldName, a => a.Value);

        var existing = rows.FirstOrDefault(r => conflictValues.All(cv =>
            r.TryGetValue(cv.Key, out var val) && Equals(val, cv.Value)));

        if (existing != null)
        {
            if (query.Strategy == UpsertStrategy.DoNothing)
                return OrmQueryResult.Ok([new Dictionary<string, object?>(existing)]);

            var updateAssignments = query.UpdateAssignments.Count > 0
                ? query.UpdateAssignments
                : query.InsertAssignments.Where(a => !query.ConflictFields.Contains(a.FieldName)).ToList();

            foreach (var assignment in updateAssignments) existing[assignment.FieldName] = assignment.Value;

            return OrmQueryResult.Ok([new Dictionary<string, object?>(existing)]);
        }

        var newRow = new Dictionary<string, object?>();
        foreach (var assignment in query.InsertAssignments) newRow[assignment.FieldName] = assignment.Value;

        rows.Add(newRow);

        return OrmQueryResult.Ok([newRow]);
    }

    private static bool EvaluatePredicate(Dictionary<string, object?> row, QueryPredicate predicate)
    {
        return predicate switch
        {
            FieldEquals eq => row.TryGetValue(eq.FieldName, out var val)
                              && Equals(val, eq.Value),

            FieldGreaterThan gt => row.TryGetValue(gt.FieldName, out var val) && val != null
                                                                              && Convert.ToDouble(val) >
                                                                              Convert.ToDouble(gt.Value),

            FieldLessThan lt => row.TryGetValue(lt.FieldName, out var val) && val != null
                                                                           && Convert.ToDouble(val) <
                                                                           Convert.ToDouble(lt.Value),

            FieldContains ct => row.TryGetValue(ct.FieldName, out var val) && val != null
                                                                           && val.ToString()!.Contains(
                                                                               ct.Value.ToString()!,
                                                                               StringComparison.OrdinalIgnoreCase),

            FieldIn i => row.TryGetValue(i.FieldName, out var val)
                         && i.Values.Any(v => Equals(val, v)),

            AndPredicate and => EvaluatePredicate(row, and.Left)
                                && EvaluatePredicate(row, and.Right),

            OrPredicate or => EvaluatePredicate(row, or.Left)
                              || EvaluatePredicate(row, or.Right),

            NotPredicate not => !EvaluatePredicate(row, not.Inner),

            _ => false
        };
    }

    private sealed class ObjectComparer : IComparer<object?>
    {
        public int Compare(object? x, object? y)
        {
            if (x == null && y == null) return 0;

            if (x == null) return -1;

            if (y == null) return 1;

            if (x is IComparable cx) return cx.CompareTo(y);

            return string.Compare(x.ToString(), y.ToString(), StringComparison.Ordinal);
        }
    }
}
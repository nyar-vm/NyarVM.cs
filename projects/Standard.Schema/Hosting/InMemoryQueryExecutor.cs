using System.Text.Json;

namespace Hermes.Hosting;

/// <summary>
///     内存查询执行器——基于 Atlas 内存数据库的查询执行
/// </summary>
public sealed class InMemoryQueryExecutor : IQueryExecutor
{
    private readonly IAtlasDatabase _db;

    /// <summary>
    ///     初始化 <see cref="InMemoryQueryExecutor" /> 类的新实例
    /// </summary>
    public InMemoryQueryExecutor(IAtlasDatabase db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<OrmQueryResult> ExecuteAsync(QueryExpression query, CancellationToken ct = default)
    {
        return query switch
        {
            FindQuery fq => await ExecuteFindAsync(fq, ct),
            CreateQuery cq => await ExecuteCreateAsync(cq, ct),
            UpdateQuery uq => await ExecuteUpdateAsync(uq, ct),
            DeleteQuery dq => await ExecuteDeleteAsync(dq, ct),
            AggregateQuery aq => await ExecuteAggregateAsync(aq, ct),
            CteQuery => OrmQueryResult.Fail("内存执行器不支持 CTE 查询"),
            WindowQuery => OrmQueryResult.Fail("内存执行器不支持窗口函数查询"),
            FullTextSearchQuery => OrmQueryResult.Fail("内存执行器不支持全文搜索查询"),
            UpsertQuery uq => await ExecuteUpsertAsync(uq, ct),
            _ => OrmQueryResult.Fail($"不支持的查询类型：{query.QueryKind}")
        };
    }

    /// <inheritdoc />
    public async Task<OrmQueryResult> ExecuteRawAsync(QueryExpression query, CancellationToken ct = default)
    {
        return await ExecuteAsync(query, ct);
    }

    private async Task<OrmQueryResult> ExecuteFindAsync(FindQuery query, CancellationToken ct)
    {
        var prefix = $"{query.TargetTypeName.ToLower()}:";
        var entries = await _db.GetPrefixAsync(prefix, ct);
        var rows = entries
            .Where(IsPrimaryKey)
            .Select(e => ToRow(e.Value))
            .Where(r => r is not null)
            .Cast<IReadOnlyDictionary<string, object?>>()
            .ToList();

        if (query.Predicate is not null) rows = rows.Where(r => MatchesPredicate(r, query.Predicate)).ToList();

        if (query.Ordering is not null)
            rows = query.Ordering.Descending
                ? rows.OrderByDescending(r => r.GetValueOrDefault(query.Ordering.FieldName)).ToList()
                : rows.OrderBy(r => r.GetValueOrDefault(query.Ordering.FieldName)).ToList();

        if (query.Pagination is not null)
            rows = rows.Skip(query.Pagination.Offset).Take(query.Pagination.Limit).ToList();

        return OrmQueryResult.Ok(rows);
    }

    private async Task<OrmQueryResult> ExecuteCreateAsync(CreateQuery query, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        var dict = new Dictionary<string, object?> { ["Id"] = id };
        foreach (var assignment in query.Assignments) dict[assignment.FieldName] = assignment.Value;

        var key = $"{query.TargetTypeName.ToLower()}:{id}";
        await _db.PutAsync(key, dict, ct);

        return OrmQueryResult.Ok([dict]);
    }

    private async Task<OrmQueryResult> ExecuteUpdateAsync(UpdateQuery query, CancellationToken ct)
    {
        var prefix = $"{query.TargetTypeName.ToLower()}:";
        var entries = await _db.GetPrefixAsync(prefix, ct);
        var affected = 0;

        foreach (var entry in entries.Where(IsPrimaryKey))
        {
            var existing = ToRow(entry.Value);
            if (existing is null || !MatchesPredicate(existing, query.Predicate)) continue;

            foreach (var assignment in query.Assignments) existing[assignment.FieldName] = assignment.Value;

            await _db.PutAsync(entry.Key, existing, ct);
            affected++;
        }

        return OrmQueryResult.Ok(affected);
    }

    private async Task<OrmQueryResult> ExecuteDeleteAsync(DeleteQuery query, CancellationToken ct)
    {
        var prefix = $"{query.TargetTypeName.ToLower()}:";
        var entries = await _db.GetPrefixAsync(prefix, ct);
        var affected = 0;

        foreach (var entry in entries.Where(IsPrimaryKey))
        {
            var row = ToRow(entry.Value);
            if (row is null || !MatchesPredicate(row, query.Predicate)) continue;

            if (await _db.DeleteAsync(entry.Key, ct)) affected++;
        }

        return OrmQueryResult.Ok(affected);
    }

    private async Task<OrmQueryResult> ExecuteAggregateAsync(AggregateQuery query, CancellationToken ct)
    {
        var prefix = $"{query.TargetTypeName.ToLower()}:";
        var entries = await _db.GetPrefixAsync(prefix, ct);
        var rows = entries
            .Where(IsPrimaryKey)
            .Select(e => ToRow(e.Value))
            .Where(d => d is not null)
            .Cast<Dictionary<string, object?>>()
            .ToList();

        if (query.Predicate is not null) rows = rows.Where(r => MatchesPredicate(r, query.Predicate)).ToList();

        var resultDict = new Dictionary<string, object?>();
        foreach (var op in query.Operations)
        {
            var alias = op.Alias ?? $"{op.Function.ToString().ToLower()}_{op.FieldName}";
            resultDict[alias] = op.Function switch
            {
                AggregateFunction.Count => rows.Count,
                AggregateFunction.Sum => rows.Sum(r => Convert.ToDouble(r.GetValueOrDefault(op.FieldName) ?? 0)),
                AggregateFunction.Avg => rows.Count > 0
                    ? rows.Average(r => Convert.ToDouble(r.GetValueOrDefault(op.FieldName) ?? 0))
                    : 0,
                AggregateFunction.Min => rows.Min(r => r.GetValueOrDefault(op.FieldName)),
                AggregateFunction.Max => rows.Max(r => r.GetValueOrDefault(op.FieldName)),
                _ => null
            };
        }

        return OrmQueryResult.Ok([resultDict]);
    }

    private async Task<OrmQueryResult> ExecuteUpsertAsync(UpsertQuery query, CancellationToken ct)
    {
        var prefix = $"{query.TargetTypeName.ToLower()}:";
        var entries = await _db.GetPrefixAsync(prefix, ct);

        var conflictValues = query.InsertAssignments
            .Where(a => query.ConflictFields.Contains(a.FieldName))
            .ToDictionary(a => a.FieldName, a => a.Value);

        var existing = entries
            .Where(IsPrimaryKey)
            .Select(e => ToRow(e.Value))
            .FirstOrDefault(r => r is not null && conflictValues.All(cv =>
                r.TryGetValue(cv.Key, out var val) && Equals(val, cv.Value)));

        if (existing != null)
        {
            if (query.Strategy == UpsertStrategy.DoNothing) return OrmQueryResult.Ok([existing]);

            foreach (var assignment in query.UpdateAssignments.Count > 0
                         ? query.UpdateAssignments
                         : query.InsertAssignments.Where(a => !query.ConflictFields.Contains(a.FieldName)))
                existing[assignment.FieldName] = assignment.Value;

            var existingKey = $"{query.TargetTypeName.ToLower()}:{existing.GetValueOrDefault("Id")}";
            await _db.PutAsync(existingKey, existing, ct);
            return OrmQueryResult.Ok([existing]);
        }

        var id = Guid.NewGuid();
        var dict = new Dictionary<string, object?> { ["Id"] = id };
        foreach (var assignment in query.InsertAssignments) dict[assignment.FieldName] = assignment.Value;

        var key = $"{query.TargetTypeName.ToLower()}:{id}";
        await _db.PutAsync(key, dict, ct);
        return OrmQueryResult.Ok([dict]);
    }

    private static bool IsPrimaryKey(AtlasEntry entry)
    {
        var parts = entry.Key.Split(':');
        return parts.Length == 2;
    }

    private static Dictionary<string, object?>? ToRow(byte[] value)
    {
        return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(value)?
            .ToDictionary(kv => kv.Key, kv => JsonElementToObject(kv.Value));
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.get_string(),
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(p => p.Name, p => JsonElementToObject(p.Value)),
            _ => element.ToString()
        };
    }

    private static bool MatchesPredicate(IReadOnlyDictionary<string, object?> row, QueryPredicate predicate)
    {
        return predicate switch
        {
            FieldEquals fe => row.TryGetValue(fe.FieldName, out var val) && Equals(val, fe.Value),
            FieldGreaterThan fgt => row.TryGetValue(fgt.FieldName, out var val) && Compare(val, fgt.Value) > 0,
            FieldLessThan flt => row.TryGetValue(flt.FieldName, out var val) && Compare(val, flt.Value) < 0,
            FieldContains fc => row.TryGetValue(fc.FieldName, out var val) &&
                                val?.ToString()?.Contains(fc.Value?.ToString() ?? "") == true,
            FieldIn fi => row.TryGetValue(fi.FieldName, out var val) && fi.Values?.Contains(val) == true,
            AndPredicate ap => MatchesPredicate(row, ap.Left) && MatchesPredicate(row, ap.Right),
            OrPredicate op => MatchesPredicate(row, op.Left) || MatchesPredicate(row, op.Right),
            NotPredicate np => !MatchesPredicate(row, np.Inner),
            _ => true
        };
    }

    private static int Compare(object? a, object? b)
    {
        if (a is null || b is null) return 0;

        if (a is IComparable ca)
            try
            {
                return ca.CompareTo(Convert.ChangeType(b, a.GetType()));
            }
            catch
            {
                return 0;
            }

        return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }
}
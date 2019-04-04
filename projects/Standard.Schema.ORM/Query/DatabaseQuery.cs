namespace Hermes.YYDB.Query;

public sealed class DatabaseQuery<T> where T : class
{
    private readonly QueryBuilder _builder;
    private readonly Func<QueryExpression, Task<OrmQueryResult>> _executor;

    public DatabaseQuery(Func<QueryExpression, Task<OrmQueryResult>> executor)
    {
        _builder = new QueryBuilder().From<T>();
        _executor = executor;
    }

    public DatabaseQuery<T> Filter(Func<T, QueryPredicate> predicateBuilder)
    {
        var predicate = predicateBuilder(default!);
        _builder.Where(predicate);
        return this;
    }

    public DatabaseQuery<T> Filter(QueryPredicate predicate)
    {
        _builder.Where(predicate);
        return this;
    }

    public DatabaseQuery<T> Sort(Func<T, string> fieldSelector, bool descending = false)
    {
        _builder.OrderBy(fieldSelector(default!), descending);
        return this;
    }

    public DatabaseQuery<T> Sort(string fieldName, bool descending = false)
    {
        _builder.OrderBy(fieldName, descending);
        return this;
    }

    public DatabaseQuery<T> Skip(int offset)
    {
        _builder.Skip(offset);
        return this;
    }

    public DatabaseQuery<T> Take(int limit)
    {
        _builder.Take(limit);
        return this;
    }

    public DatabaseQuery<T> Set(Func<T, string> fieldSelector, object value)
    {
        _builder.Set(fieldSelector(default!), value);
        return this;
    }

    public DatabaseQuery<T> Set(string fieldName, object value)
    {
        _builder.Set(fieldName, value);
        return this;
    }

    public Task<List<T>> ToListAsync()
    {
        var query = _builder.BuildFind();
        return ExecuteAndMapAsync(query);
    }

    public Task<T?> FirstOrDefaultAsync()
    {
        _builder.Take(1);
        var query = _builder.BuildFind();
        return ExecuteAndMapFirstAsync(query);
    }

    public Task<int> InsertAsync(T entity)
    {
        var assignments = BuildAssignmentsFromEntity(entity);
        foreach (var a in assignments) _builder.Set(a.FieldName, a.Value);

        var query = _builder.BuildCreate();
        return ExecuteAndGetCountAsync(query);
    }

    public Task<int> UpdateAsync()
    {
        var query = _builder.BuildUpdate();
        return ExecuteAndGetCountAsync(query);
    }

    public Task<int> DeleteAsync()
    {
        var query = _builder.BuildDelete();
        return ExecuteAndGetCountAsync(query);
    }

    public Task<long> CountAsync()
    {
        _builder.Count("*", "cnt");
        var query = _builder.BuildAggregate();
        return ExecuteAndGetScalarAsync(query);
    }

    private async Task<List<T>> ExecuteAndMapAsync(QueryExpression query)
    {
        var result = await _executor(query);
        if (!result.Success) throw new InvalidOperationException($"查询失败：{result.Error}");

        return MapRows(result.Rows);
    }

    private async Task<T?> ExecuteAndMapFirstAsync(QueryExpression query)
    {
        var result = await _executor(query);
        if (!result.Success) throw new InvalidOperationException($"查询失败：{result.Error}");

        var rows = MapRows(result.Rows);
        return rows.Count > 0 ? rows[0] : null;
    }

    private async Task<int> ExecuteAndGetCountAsync(QueryExpression query)
    {
        var result = await _executor(query);
        if (!result.Success) throw new InvalidOperationException($"操作失败：{result.Error}");

        return result.AffectedCount;
    }

    private async Task<long> ExecuteAndGetScalarAsync(QueryExpression query)
    {
        var result = await _executor(query);
        if (!result.Success) throw new InvalidOperationException($"聚合失败：{result.Error}");

        if (result.Rows.Count > 0)
        {
            var firstRow = result.Rows[0];
            var firstValue = firstRow.Values.FirstOrDefault();
            if (firstValue is long l) return l;

            if (long.TryParse(firstValue?.ToString(), out var parsed)) return parsed;
        }

        return 0;
    }

    private static List<T> MapRows(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        var type = typeof(T);
        var list = new List<T>();

        foreach (var row in rows)
        {
            var obj = Activator.CreateInstance<T>();
            foreach (var prop in type.GetProperties())
                if (row.TryGetValue(prop.Name, out var value) && value != null)
                {
                    var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                    var converted = Convert.ChangeType(value, targetType);
                    prop.SetValue(obj, converted);
                }

            list.Add(obj);
        }

        return list;
    }

    private static List<FieldAssignment> BuildAssignmentsFromEntity(T entity)
    {
        var assignments = new List<FieldAssignment>();
        var type = typeof(T);

        foreach (var prop in type.GetProperties())
        {
            var value = prop.GetValue(entity);
            if (value != null) assignments.Add(new FieldAssignment(prop.Name, value));
        }

        return assignments;
    }
}
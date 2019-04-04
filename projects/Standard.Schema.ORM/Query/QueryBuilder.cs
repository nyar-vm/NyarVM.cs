namespace Hermes.YYDB.Query;

public sealed class QueryBuilder
{
    private readonly List<AggregateOperation> _aggregates = [];
    private readonly List<FieldAssignment> _assignments = [];
    private readonly List<string> _groupBy = [];
    private QueryOrdering? _ordering;
    private QueryPagination? _pagination;
    private QueryPredicate? _predicate;
    private string? _targetTypeName;

    public QueryBuilder From<T>() where T : class
    {
        _targetTypeName = typeof(T).Name;
        return this;
    }

    public QueryBuilder From(string typeName)
    {
        _targetTypeName = typeName;
        return this;
    }

    public QueryBuilder Where(QueryPredicate predicate)
    {
        _predicate = _predicate == null ? predicate : new AndPredicate(_predicate, predicate);
        return this;
    }

    public QueryBuilder OrderBy(string fieldName, bool descending = false)
    {
        _ordering = new QueryOrdering(fieldName, descending);
        return this;
    }

    public QueryBuilder Skip(int offset)
    {
        _pagination ??= new QueryPagination();
        _pagination = new QueryPagination(offset, _pagination.Limit);
        return this;
    }

    public QueryBuilder Take(int limit)
    {
        _pagination ??= new QueryPagination();
        _pagination = new QueryPagination(_pagination.Offset, limit);
        return this;
    }

    public QueryBuilder Set(string fieldName, object value)
    {
        _assignments.Add(new FieldAssignment(fieldName, value));
        return this;
    }

    public QueryBuilder Count(string fieldName, string? alias = null)
    {
        _aggregates.Add(new AggregateOperation(AggregateFunction.Count, fieldName, alias));
        return this;
    }

    public QueryBuilder Sum(string fieldName, string? alias = null)
    {
        _aggregates.Add(new AggregateOperation(AggregateFunction.Sum, fieldName, alias));
        return this;
    }

    public QueryBuilder Avg(string fieldName, string? alias = null)
    {
        _aggregates.Add(new AggregateOperation(AggregateFunction.Avg, fieldName, alias));
        return this;
    }

    public QueryBuilder Min(string fieldName, string? alias = null)
    {
        _aggregates.Add(new AggregateOperation(AggregateFunction.Min, fieldName, alias));
        return this;
    }

    public QueryBuilder Max(string fieldName, string? alias = null)
    {
        _aggregates.Add(new AggregateOperation(AggregateFunction.Max, fieldName, alias));
        return this;
    }

    public QueryBuilder GroupBy(params string[] fieldNames)
    {
        _groupBy.AddRange(fieldNames);
        return this;
    }

    public FindQuery BuildFind()
    {
        if (_targetTypeName is null) throw new InvalidOperationException("必须指定查询目标类型");

        return new FindQuery(_targetTypeName, _predicate, _ordering, _pagination);
    }

    public CreateQuery BuildCreate()
    {
        if (_targetTypeName is null) throw new InvalidOperationException("必须指定创建目标类型");

        if (_assignments.Count == 0) throw new InvalidOperationException("必须指定至少一个字段赋值");

        return new CreateQuery(_targetTypeName, _assignments);
    }

    public UpdateQuery BuildUpdate()
    {
        if (_targetTypeName is null) throw new InvalidOperationException("必须指定更新目标类型");

        if (_predicate is null) throw new InvalidOperationException("更新操作必须指定查询条件");

        if (_assignments.Count == 0) throw new InvalidOperationException("必须指定至少一个字段赋值");

        return new UpdateQuery(_targetTypeName, _predicate, _assignments);
    }

    public DeleteQuery BuildDelete()
    {
        if (_targetTypeName is null) throw new InvalidOperationException("必须指定删除目标类型");

        if (_predicate is null) throw new InvalidOperationException("删除操作必须指定查询条件");

        return new DeleteQuery(_targetTypeName, _predicate);
    }

    public AggregateQuery BuildAggregate()
    {
        if (_targetTypeName is null) throw new InvalidOperationException("必须指定聚合源类型");

        if (_aggregates.Count == 0) throw new InvalidOperationException("必须指定至少一个聚合操作");

        return new AggregateQuery(_targetTypeName, _aggregates, _predicate, _groupBy);
    }
}
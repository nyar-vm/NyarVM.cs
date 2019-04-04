namespace Hermes.YYDB.Query;

public sealed class FieldAssignment
{
    public FieldAssignment(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public string FieldName { get; }
    public object Value { get; }
}

public sealed class QueryOrdering
{
    public QueryOrdering(string fieldName, bool descending = false)
    {
        FieldName = fieldName;
        Descending = descending;
    }

    public string FieldName { get; }
    public bool Descending { get; }
}

public sealed class QueryPagination
{
    public QueryPagination(int offset = 0, int limit = 100)
    {
        Offset = offset;
        Limit = limit;
    }

    public int Offset { get; }
    public int Limit { get; }
}

public sealed class AggregateOperation
{
    public AggregateOperation(AggregateFunction function, string fieldName, string? alias = null)
    {
        Function = function;
        FieldName = fieldName;
        Alias = alias;
    }

    public AggregateFunction Function { get; }
    public string FieldName { get; }
    public string? Alias { get; }
}

public enum AggregateFunction
{
    Count,
    Sum,
    Avg,
    Min,
    Max,
    First,
    Last,
    Distinct
}
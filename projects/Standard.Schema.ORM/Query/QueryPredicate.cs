namespace Hermes.YYDB.Query;

public abstract class QueryPredicate
{
    public abstract string PredicateKind { get; }
}

public sealed class FieldEquals : QueryPredicate
{
    public FieldEquals(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public override string PredicateKind => "equals";
    public string FieldName { get; }
    public object Value { get; }
}

public sealed class FieldGreaterThan : QueryPredicate
{
    public FieldGreaterThan(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public override string PredicateKind => "greater_than";
    public string FieldName { get; }
    public object Value { get; }
}

public sealed class FieldLessThan : QueryPredicate
{
    public FieldLessThan(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public override string PredicateKind => "less_than";
    public string FieldName { get; }
    public object Value { get; }
}

public sealed class FieldContains : QueryPredicate
{
    public FieldContains(string fieldName, object value)
    {
        FieldName = fieldName;
        Value = value;
    }

    public override string PredicateKind => "contains";
    public string FieldName { get; }
    public object Value { get; }
}

public sealed class FieldIn : QueryPredicate
{
    public FieldIn(string fieldName, IReadOnlyList<object> values)
    {
        FieldName = fieldName;
        Values = values;
    }

    public override string PredicateKind => "in";
    public string FieldName { get; }
    public IReadOnlyList<object> Values { get; }
}

public sealed class AndPredicate : QueryPredicate
{
    public AndPredicate(QueryPredicate left, QueryPredicate right)
    {
        Left = left;
        Right = right;
    }

    public override string PredicateKind => "and";
    public QueryPredicate Left { get; }
    public QueryPredicate Right { get; }
}

public sealed class OrPredicate : QueryPredicate
{
    public OrPredicate(QueryPredicate left, QueryPredicate right)
    {
        Left = left;
        Right = right;
    }

    public override string PredicateKind => "or";
    public QueryPredicate Left { get; }
    public QueryPredicate Right { get; }
}

public sealed class NotPredicate : QueryPredicate
{
    public NotPredicate(QueryPredicate inner)
    {
        Inner = inner;
    }

    public override string PredicateKind => "not";
    public QueryPredicate Inner { get; }
}
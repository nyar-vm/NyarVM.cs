namespace Hermes.YYDB.Query;

public sealed class AggregateQuery : QueryExpression
{
    public AggregateQuery(string sourceTypeName, IReadOnlyList<AggregateOperation> operations,
        QueryPredicate? predicate = null, IReadOnlyList<string>? groupBy = null)
        : base(sourceTypeName)
    {
        Operations = operations;
        Predicate = predicate;
        GroupBy = groupBy ?? [];
    }

    public override string QueryKind => "aggregate";
    public IReadOnlyList<AggregateOperation> Operations { get; }
    public QueryPredicate? Predicate { get; }
    public IReadOnlyList<string> GroupBy { get; }
}
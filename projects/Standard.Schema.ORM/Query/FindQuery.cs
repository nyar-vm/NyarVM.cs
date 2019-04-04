namespace Hermes.YYDB.Query;

public sealed class FindQuery : QueryExpression
{
    public FindQuery(string targetTypeName, QueryPredicate? predicate = null, QueryOrdering? ordering = null,
        QueryPagination? pagination = null)
        : base(targetTypeName)
    {
        Predicate = predicate;
        Ordering = ordering;
        Pagination = pagination;
    }

    public override string QueryKind => "find";
    public QueryPredicate? Predicate { get; }
    public QueryOrdering? Ordering { get; }
    public QueryPagination? Pagination { get; }
}
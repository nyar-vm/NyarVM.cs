namespace Hermes.YYDB.Query;

public sealed class DeleteQuery : QueryExpression
{
    public DeleteQuery(string targetTypeName, QueryPredicate predicate)
        : base(targetTypeName)
    {
        Predicate = predicate;
    }

    public override string QueryKind => "delete";
    public QueryPredicate Predicate { get; }
}
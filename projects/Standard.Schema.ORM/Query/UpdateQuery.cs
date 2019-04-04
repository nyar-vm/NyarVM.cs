namespace Hermes.YYDB.Query;

public sealed class UpdateQuery : QueryExpression
{
    public UpdateQuery(string targetTypeName, QueryPredicate predicate, IReadOnlyList<FieldAssignment> assignments)
        : base(targetTypeName)
    {
        Predicate = predicate;
        Assignments = assignments;
    }

    public override string QueryKind => "update";
    public QueryPredicate Predicate { get; }
    public IReadOnlyList<FieldAssignment> Assignments { get; }
}
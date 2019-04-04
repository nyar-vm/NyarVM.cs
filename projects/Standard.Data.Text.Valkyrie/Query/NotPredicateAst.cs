namespace Std.Data.Text.Valkyrie.Query;

public sealed class NotPredicateAst : QueryPredicateAst
{
    public NotPredicateAst(QueryPredicateAst inner)
    {
        this.inner = inner;
    }

    public override string predicate_kind => "not";
    public QueryPredicateAst inner { get; }
}
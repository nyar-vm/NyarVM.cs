namespace Std.Data.Text.Valkyrie.Query;

public sealed class AndPredicateAst : QueryPredicateAst
{
    public AndPredicateAst(QueryPredicateAst left, QueryPredicateAst right)
    {
        this.left = left;
        this.right = right;
    }

    public override string predicate_kind => "and";
    public QueryPredicateAst left { get; }
    public QueryPredicateAst right { get; }
}
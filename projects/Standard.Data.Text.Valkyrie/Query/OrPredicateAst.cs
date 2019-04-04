namespace Std.Data.Text.Valkyrie.Query;

public sealed class OrPredicateAst : QueryPredicateAst
{
    public OrPredicateAst(QueryPredicateAst left, QueryPredicateAst right)
    {
        this.left = left;
        this.right = right;
    }

    public override string predicate_kind => "or";
    public QueryPredicateAst left { get; }
    public QueryPredicateAst right { get; }
}
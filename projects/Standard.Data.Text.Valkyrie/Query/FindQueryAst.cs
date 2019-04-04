namespace Std.Data.Text.Valkyrie.Query;

public sealed class FindQueryAst : QueryAstNode
{
    public FindQueryAst(string targetTypeName, QueryPredicateAst? predicate = null, QueryOrderingAst? ordering = null,
        QueryPaginationAst? pagination = null)
    {
        target_type_name = targetTypeName;
        this.predicate = predicate;
        this.ordering = ordering;
        this.pagination = pagination;
    }

    public override string node_kind => "find";
    public string target_type_name { get; }
    public QueryPredicateAst? predicate { get; }
    public QueryOrderingAst? ordering { get; }
    public QueryPaginationAst? pagination { get; }
}
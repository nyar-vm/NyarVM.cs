namespace Std.Data.Text.Valkyrie.Query;

public sealed class DeleteQueryAst : QueryAstNode
{
    public DeleteQueryAst(string targetTypeName, QueryPredicateAst predicate)
    {
        target_type_name = targetTypeName;
        this.predicate = predicate;
    }

    public override string node_kind => "delete";
    public string target_type_name { get; }
    public QueryPredicateAst predicate { get; }
}
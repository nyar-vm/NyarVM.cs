namespace Std.Data.Text.Valkyrie.Query;

public sealed class UpdateQueryAst : QueryAstNode
{
    public UpdateQueryAst(string targetTypeName, QueryPredicateAst predicate,
        IReadOnlyList<FieldAssignmentAst> assignments)
    {
        target_type_name = targetTypeName;
        this.predicate = predicate;
        this.assignments = assignments;
    }

    public override string node_kind => "update";
    public string target_type_name { get; }
    public QueryPredicateAst predicate { get; }
    public IReadOnlyList<FieldAssignmentAst> assignments { get; }
}
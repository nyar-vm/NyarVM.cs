namespace Std.Data.Text.Valkyrie.Query;

public sealed class AggregateQueryAst : QueryAstNode
{
    public AggregateQueryAst(string sourceTypeName, IReadOnlyList<AggregateOperationAst> operations,
        QueryPredicateAst? predicate = null, IReadOnlyList<string>? groupBy = null)
    {
        source_type_name = sourceTypeName;
        this.operations = operations;
        this.predicate = predicate;
        group_by = groupBy ?? [];
    }

    public override string node_kind => "aggregate";
    public string source_type_name { get; }
    public IReadOnlyList<AggregateOperationAst> operations { get; }
    public QueryPredicateAst? predicate { get; }
    public IReadOnlyList<string> group_by { get; }
}
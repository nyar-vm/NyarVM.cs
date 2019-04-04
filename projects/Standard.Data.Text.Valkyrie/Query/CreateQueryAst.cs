namespace Std.Data.Text.Valkyrie.Query;

public sealed class CreateQueryAst : QueryAstNode
{
    public CreateQueryAst(string targetTypeName, IReadOnlyList<FieldAssignmentAst> assignments)
    {
        target_type_name = targetTypeName;
        this.assignments = assignments;
    }

    public override string node_kind => "create";
    public string target_type_name { get; }
    public IReadOnlyList<FieldAssignmentAst> assignments { get; }
}
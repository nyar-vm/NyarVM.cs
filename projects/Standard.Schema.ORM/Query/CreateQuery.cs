namespace Hermes.YYDB.Query;

public sealed class CreateQuery : QueryExpression
{
    public CreateQuery(string targetTypeName, IReadOnlyList<FieldAssignment> assignments)
        : base(targetTypeName)
    {
        Assignments = assignments;
    }

    public override string QueryKind => "create";
    public IReadOnlyList<FieldAssignment> Assignments { get; }
}
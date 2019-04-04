namespace Std.Data.Text.Valkyrie.Query;

public sealed class FieldInAst : QueryPredicateAst
{
    public FieldInAst(string fieldName, IReadOnlyList<object> values)
    {
        field_name = fieldName;
        this.values = values;
    }

    public override string predicate_kind => "in";
    public string field_name { get; }
    public IReadOnlyList<object> values { get; }
}
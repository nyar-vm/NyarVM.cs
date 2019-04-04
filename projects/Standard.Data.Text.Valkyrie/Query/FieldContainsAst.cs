namespace Std.Data.Text.Valkyrie.Query;

public sealed class FieldContainsAst : QueryPredicateAst
{
    public FieldContainsAst(string fieldName, object value)
    {
        field_name = fieldName;
        this.value = value;
    }

    public override string predicate_kind => "contains";
    public string field_name { get; }
    public object value { get; }
}
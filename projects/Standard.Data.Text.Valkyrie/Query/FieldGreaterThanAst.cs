namespace Std.Data.Text.Valkyrie.Query;

public sealed class FieldGreaterThanAst : QueryPredicateAst
{
    public FieldGreaterThanAst(string fieldName, object value)
    {
        field_name = fieldName;
        this.value = value;
    }

    public override string predicate_kind => "greater_than";
    public string field_name { get; }
    public object value { get; }
}
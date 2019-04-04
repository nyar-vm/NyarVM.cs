namespace Std.Data.Text.Valkyrie.Query;

public sealed class FieldLessThanAst : QueryPredicateAst
{
    public FieldLessThanAst(string fieldName, object value)
    {
        field_name = fieldName;
        this.value = value;
    }

    public override string predicate_kind => "less_than";
    public string field_name { get; }
    public object value { get; }
}
namespace Std.Data.Text.Valkyrie.Query;

public sealed class FieldEqualsAst : QueryPredicateAst
{
    public FieldEqualsAst(string fieldName, object value)
    {
        field_name = fieldName;
        this.value = value;
    }

    public override string predicate_kind => "equals";
    public string field_name { get; }
    public object value { get; }
}
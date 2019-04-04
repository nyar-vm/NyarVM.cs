namespace Std.Data.Text.Valkyrie.Query;

public sealed class FieldAssignmentAst
{
    public FieldAssignmentAst(string fieldName, object value)
    {
        field_name = fieldName;
        this.value = value;
    }

    public string field_name { get; }
    public object value { get; }
}
namespace Std.Data.Text.Valkyrie.Query;

public sealed class QueryOrderingAst
{
    public QueryOrderingAst(string fieldName, bool descending = false)
    {
        field_name = fieldName;
        this.descending = descending;
    }

    public string field_name { get; }
    public bool descending { get; }
}
namespace Std.Data.Text.Valkyrie.Query;

public sealed class QueryToken
{
    public QueryToken(QueryTokenType type, string value, int position)
    {
        this.type = type;
        this.value = value;
        this.position = position;
    }

    public QueryTokenType type { get; }
    public string value { get; }
    public int position { get; }

    public override string ToString()
    {
        return $"{type}({value})";
    }
}
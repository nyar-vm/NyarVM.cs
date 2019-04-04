namespace Std.Data.Text.Valkyrie.Query;

public sealed class QueryParseException : Exception
{
    public QueryParseException(int position, string message) : base($"位置 {position}: {message}")
    {
        this.position = position;
    }

    public int position { get; }
}
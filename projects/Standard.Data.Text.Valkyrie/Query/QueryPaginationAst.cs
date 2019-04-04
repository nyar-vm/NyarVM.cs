namespace Std.Data.Text.Valkyrie.Query;

public sealed class QueryPaginationAst
{
    public QueryPaginationAst(int offset = 0, int limit = 100)
    {
        this.offset = offset;
        this.limit = limit;
    }

    public int offset { get; }
    public int limit { get; }
}
namespace Hermes.YYDB.Query;

public sealed class OrmQueryResult
{
    public bool Success { get; init; }
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];
    public int AffectedCount { get; init; }
    public string? Error { get; init; }

    public static OrmQueryResult Ok(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows)
    {
        return new OrmQueryResult { Success = true, Rows = rows };
    }

    public static OrmQueryResult Ok(int affectedCount)
    {
        return new OrmQueryResult { Success = true, AffectedCount = affectedCount };
    }

    public static OrmQueryResult Fail(string error)
    {
        return new OrmQueryResult { Success = false, Error = error };
    }
}
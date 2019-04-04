using Hermes.Database.Schema;
using Hermes.YYDB.Query;

namespace Hermes.Database.Frontend;

public interface IUnifiedStorage
{
    string Name { get; }
    StorageCapabilities Capabilities { get; }

    Task<QueryResult> ExecuteAsync(QueryExpression query, CancellationToken ct = default);
    Task<QueryResult> ExecuteAsync(QueryExpression query, string backendHint, CancellationToken ct = default);
    Task<BatchResult> ExecuteBatchAsync(IReadOnlyList<QueryExpression> queries, CancellationToken ct = default);
    IUnifiedStorage On(string backendName);
}

public sealed class StorageCapabilities
{
    public bool SupportsCreate { get; init; }
    public bool SupportsRead { get; init; }
    public bool SupportsUpdate { get; init; }
    public bool SupportsDelete { get; init; }
    public bool SupportsAggregate { get; init; }
    public bool SupportsBatch { get; init; }
    public bool SupportsTransaction { get; init; }
    public bool SupportsStreaming { get; init; }
    public IReadOnlyList<StorageBackendKind> AvailableBackends { get; init; } = [];
}

public sealed class QueryResult
{
    public bool Success { get; init; }
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; init; } = [];
    public int AffectedCount { get; init; }
    public string? BackendUsed { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public string? Error { get; init; }

    public static QueryResult Ok(IReadOnlyList<IReadOnlyDictionary<string, object?>> rows, string? backend = null)
    {
        return new QueryResult { Success = true, Rows = rows, BackendUsed = backend };
    }

    public static QueryResult Ok(int affectedCount, string? backend = null)
    {
        return new QueryResult { Success = true, AffectedCount = affectedCount, BackendUsed = backend };
    }

    public static QueryResult Fail(string error)
    {
        return new QueryResult { Success = false, Error = error };
    }
}

public sealed class BatchResult
{
    public IReadOnlyList<QueryResult> Results { get; init; } = [];
    public bool AllSuccess => Results.All(r => r.Success);
    public int SuccessCount => Results.Count(r => r.Success);
    public int FailCount => Results.Count(r => !r.Success);
}
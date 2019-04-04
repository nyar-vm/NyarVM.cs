using Hermes.Database.Schema;
using Hermes.YYDB.Query;

namespace Hermes.Database.Frontend;

public interface IStorageAdapter
{
    StorageBackendKind Kind { get; }
    string Name { get; }

    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task<QueryResult> ReadAsync(QueryExpression query, CancellationToken ct = default);
    Task<QueryResult> WriteAsync(QueryExpression query, CancellationToken ct = default);
    Task<QueryResult> DeleteAsync(QueryExpression query, CancellationToken ct = default);
    Task<QueryResult> AggregateAsync(QueryExpression query, CancellationToken ct = default);
}

public interface IAggregationEngine
{
    Task SyncAsync(AggregationPlan plan, CancellationToken ct = default);
    Task<QueryResult> QueryAggregatedAsync(string planName, QueryExpression query, CancellationToken ct = default);
    AggregationPlan? GetPlan(string planName);
    void RegisterPlan(AggregationPlan plan);
}
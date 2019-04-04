using System.Collections.Concurrent;
using Core.Security.Audit;

namespace Std.Security.Audit;

/// <summary>
///     内存审计记录存储，实现 <see cref="IAuditStore" /> 接口，
///     提供异步写入和查询审计记录的能力。
/// </summary>
public sealed class InMemoryAuditStore : IAuditStore
{
    private readonly ConcurrentQueue<IAuditEntry> _entries = new();

    /// <summary>
    ///     异步写入审计记录到内存存储。
    /// </summary>
    /// <param name="entry">审计记录。</param>
    public Task write(IAuditEntry entry)
    {
        _entries.Enqueue(entry);
        return Task.CompletedTask;
    }

    /// <summary>
    ///     异步查询审计记录，按指定条件过滤。
    /// </summary>
    /// <param name="filter">过滤谓词，为 null 时返回所有记录。</param>
    /// <returns>匹配的审计记录列表。</returns>
    public Task<List<IAuditEntry>> query(Func<IAuditEntry, bool>? filter = null)
    {
        if (filter is null) return Task.FromResult(new List<IAuditEntry>(_entries));

        var results = new List<IAuditEntry>();

        foreach (var entry in _entries)
            if (filter(entry))
                results.Add(entry);

        return Task.FromResult(results);
    }
}
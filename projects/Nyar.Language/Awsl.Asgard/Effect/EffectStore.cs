using System.Text.Json;

namespace Valkyrie.Asgard.Effect;

/// <summary>
///     Effect 副作用存储，管理 pending/resolved/rejected 条目的生命周期
/// </summary>
public sealed class EffectStore
{
    private readonly Dictionary<string, EffectEntry> _entries = new();
    private readonly Dictionary<string, long> _timestamps = new();

    /// <summary>等待中的 Effect 数量</summary>
    public int pending_count => _entries.Values.Count(e => e.status == EffectStatus.pending);

    /// <summary>已解决的 Effect 数量</summary>
    public int resolved_count => _entries.Values.Count(e => e.status == EffectStatus.resolved);

    /// <summary>已拒绝的 Effect 数量</summary>
    public int rejected_count => _entries.Values.Count(e => e.status == EffectStatus.rejected);

    /// <summary>
    ///     注册一个新的 Effect 条目
    /// </summary>
    /// <param name="functionName">副作用函数名</param>
    /// <param name="args">函数参数</param>
    /// <param name="config">副作用配置</param>
    /// <returns>Effect ID，失败返回 null</returns>
    public string? register(string functionName, string[] args, EffectConfig config)
    {
        var id = $"ef-{Guid.NewGuid():N}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        _entries[id] = new EffectEntry
        {
            id = id,
            function_name = functionName,
            args = args,
            status = EffectStatus.pending,
            created_at = timestamp,
            updated_at = timestamp
        };

        _timestamps[id] = timestamp;
        return id;
    }

    /// <summary>
    ///     解决一个 Effect 条目
    /// </summary>
    /// <param name="entryId">Effect ID</param>
    /// <param name="result">解决结果</param>
    public void resolve(string entryId, JsonElement result)
    {
        if (!_entries.TryGetValue(entryId, out var entry))
        {
            return;
        }

        _entries[entryId] = entry with
        {
            status = EffectStatus.resolved,
            result = result,
            updated_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    ///     拒绝一个 Effect 条目
    /// </summary>
    /// <param name="entryId">Effect ID</param>
    /// <param name="error">拒绝原因</param>
    public void reject(string entryId, string error)
    {
        if (!_entries.TryGetValue(entryId, out var entry))
        {
            return;
        }

        _entries[entryId] = entry with
        {
            status = EffectStatus.rejected,
            error = error,
            updated_at = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    /// <summary>
    ///     获取 Effect 结果
    /// </summary>
    /// <param name="entryId">Effect ID</param>
    /// <returns>Effect 结果，不存在返回 null</returns>
    public EffectResult? get_result(string entryId)
    {
        if (!_entries.TryGetValue(entryId, out var entry))
        {
            return null;
        }

        return new EffectResult
        {
            status = entry.status,
            data = entry.result,
            error = entry.status == EffectStatus.rejected ? entry.error : null,
            entry_id = entryId
        };
    }

    /// <summary>
    ///     使超时的 Pending Effect 过期
    /// </summary>
    /// <param name="now">当前时间戳（毫秒）</param>
    public void expire(long now)
    {
        foreach (var (id, timestamp) in _timestamps)
        {
            if (now - timestamp > 0 && _entries.TryGetValue(id, out var entry) && entry.status == EffectStatus.pending)
            {
                _entries[id] = entry with
                {
                    status = EffectStatus.rejected,
                    error = "timeout",
                    updated_at = now
                };
            }
        }
    }
}

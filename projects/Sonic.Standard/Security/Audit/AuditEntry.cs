using Core.Security.Audit;

namespace Std.Security.Audit;

/// <summary>
///     审计记录条目，实现 <see cref="IAuditEntry" /> 接口。
/// </summary>
public sealed class AuditEntry : IAuditEntry
{
    /// <summary>
    ///     初始化 <see cref="AuditEntry" /> 的新实例。
    /// </summary>
    /// <param name="action">审计动作。</param>
    /// <param name="timestamp">审计时间戳。</param>
    /// <param name="userId">操作用户标识。</param>
    /// <param name="category">审计分类。</param>
    public AuditEntry(string action, DateTimeOffset timestamp, string userId, string? category = null)
    {
        this.action = action;
        this.timestamp = timestamp;
        user_id = userId;
        this.category = category;
    }

    /// <summary>
    ///     审计分类。
    /// </summary>
    public string? category { get; }

    /// <summary>
    ///     审计动作。
    /// </summary>
    public string action { get; }

    /// <summary>
    ///     审计时间戳。
    /// </summary>
    public DateTimeOffset timestamp { get; }

    /// <summary>
    ///     操作用户标识。
    /// </summary>
    public string user_id { get; }
}
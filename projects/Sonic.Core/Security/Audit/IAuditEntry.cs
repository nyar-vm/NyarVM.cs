using System;

namespace Core.Security.Audit;

/// <summary>
///     IAuditEntry 接口
/// </summary>
public interface IAuditEntry
{
    /// <summary>
    ///     审计动作
    /// </summary>
    string action { get; }

    /// <summary>
    ///     审计时间戳
    /// </summary>
    DateTimeOffset timestamp { get; }

    /// <summary>
    ///     操作用户标识
    /// </summary>
    string user_id { get; }
}
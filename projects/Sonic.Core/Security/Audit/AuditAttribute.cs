using System;

namespace Core.Security.Audit;

/// <summary>
///     标记一个方法需要审计日志记录
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class AuditAttribute : Attribute
{
    /// <summary>
    ///     审计动作名称
    /// </summary>
    public string? action { get; set; }
}
using System;

namespace Core.Security.Audit;

/// <summary>
///     Auditable 属性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuditableAttribute : Attribute
{
    /// <summary>
    ///     审计分类
    /// </summary>
    public string? category { get; set; }
}
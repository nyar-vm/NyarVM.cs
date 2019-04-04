using System;

namespace Core.Security.Audit;

/// <summary>
///     AuditField 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditFieldAttribute : Attribute
{
    /// <summary>
    ///     字段显示名称
    /// </summary>
    public string? name { get; set; }
}
using System;

namespace Core.Security.Audit;

/// <summary>
///     AuditIgnore 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AuditIgnoreAttribute : Attribute
{
}
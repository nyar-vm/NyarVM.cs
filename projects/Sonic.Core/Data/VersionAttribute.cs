using System;

namespace Core.Data;

/// <summary>
///     标记属性为版本号字段，用于乐观并发控制。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class VersionAttribute : Attribute
{
}
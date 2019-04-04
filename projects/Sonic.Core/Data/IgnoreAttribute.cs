using System;

namespace Core.Data;

/// <summary>
///     标记共享数据字段在自动投影/序列化中忽略。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class IgnoreAttribute : Attribute
{
}

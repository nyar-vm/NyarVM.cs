using System;

namespace Core.Data;

/// <summary>
///     标记属性参与乐观并发检查，更新时验证版本未变更。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ConcurrencyCheckAttribute : Attribute
{
}
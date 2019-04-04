using System;

namespace Core.Data;

/// <summary>
///     标记属性在存储映射时忽略，不参与持久化。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class IgnoreStorageAttribute : Attribute
{
}
using System;

namespace Core.Data.Key;

/// <summary>
///     标记属性为主键字段。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class KeyAttribute : Attribute
{
}
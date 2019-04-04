using System;

namespace Core.Data;

/// <summary>
///     标记属性包含敏感数据，序列化时应进行脱敏处理。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SensitiveAttribute : Attribute
{
}
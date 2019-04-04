using System;

namespace Core.Chrono;

/// <summary>
///     Timestamp 属性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class TimestampAttribute : Attribute
{
}
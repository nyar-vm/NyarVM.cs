using System;

namespace Core.Concurrency;

/// <summary>
///     标记字段或属性为通道，支持指定容量
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ChannelAttribute : Attribute
{
    /// <summary>
    ///     通道容量，0 表示无界通道
    /// </summary>
    public int capacity { get; set; }
}
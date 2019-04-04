using System;
using Core.Marker;

namespace Core.Infra;

/// <summary>
///     标记一个字段或属性为消息队列资源
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class QueueAttribute : Attribute
{
    /// <summary>
    ///     队列名称
    /// </summary>
    public string? name { get; set; }

    /// <summary>
    ///     队列可见性
    /// </summary>
    public SonicVisibility visibility { get; set; } = SonicVisibility.@public;
}
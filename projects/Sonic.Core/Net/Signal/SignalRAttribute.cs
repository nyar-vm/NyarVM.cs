using System;

namespace Core.Net.Signal;

/// <summary>
///     SignalR 属性
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class SignalRAttribute : Attribute
{
    /// <summary>
    ///     Hub 名称
    /// </summary>
    public string? hub_name { get; set; }
}
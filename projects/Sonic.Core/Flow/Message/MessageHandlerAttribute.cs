using System;

namespace Core.Flow.Message;

/// <summary>
///     标记方法为消息处理器
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MessageHandlerAttribute : Attribute
{
    /// <summary>
    ///     消息主题
    /// </summary>
    public string? topic { get; set; }
}
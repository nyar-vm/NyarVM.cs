using System;

namespace Core.Flow.Message;

/// <summary>
///     标记方法为事件处理器
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EventHandlerAttribute : Attribute
{
}
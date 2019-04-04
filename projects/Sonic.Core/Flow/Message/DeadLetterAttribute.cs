using System;

namespace Core.Flow.Message;

/// <summary>
///     标记方法或类为死信处理器
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class DeadLetterAttribute : Attribute
{
}
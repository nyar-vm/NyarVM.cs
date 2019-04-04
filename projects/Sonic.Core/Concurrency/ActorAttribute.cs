using System;

namespace Core.Concurrency;

/// <summary>
///     标记类为 Actor 模型参与者
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ActorAttribute : Attribute
{
}
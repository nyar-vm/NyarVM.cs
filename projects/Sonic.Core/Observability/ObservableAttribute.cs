using System;

namespace Core.Observability;

/// <summary>
///     标记一个类或方法为可观测对象
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ObservableAttribute : Attribute
{
}
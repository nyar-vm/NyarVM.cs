using System;

namespace Core.Flow.Pipeline;

/// <summary>
///     StreamSource 属性
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class)]
public sealed class StreamSourceAttribute : Attribute
{
}
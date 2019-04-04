using System;

namespace Core.DI;

/// <summary>
///     标记类为依赖注入服务
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceAttribute : Attribute
{
    /// <summary>
    ///     服务生命周期
    /// </summary>
    public ServiceLifetime lifetime { get; set; } = ServiceLifetime.transient;
}
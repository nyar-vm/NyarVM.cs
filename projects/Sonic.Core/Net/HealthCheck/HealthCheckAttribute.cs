using System;

namespace Core.Net.HealthCheck;

/// <summary>
///     标记一个类为健康检查探针，生成器将自动注册。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class HealthCheckAttribute : Attribute
{
    /// <summary>
    ///     健康检查的名称。
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     是否为 Liveness 探针。
    /// </summary>
    public bool Liveness { get; set; } = true;

    /// <summary>
    ///     是否为 Readiness 探针。
    /// </summary>
    public bool Readiness { get; set; } = true;
}
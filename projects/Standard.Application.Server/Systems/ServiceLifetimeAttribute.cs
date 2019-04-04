using Microsoft.Extensions.DependencyInjection;

namespace Std.App.Server.Systems;

/// <summary>
///     覆盖 Atlas 系统默认的 Singleton 生命周期。
///     默认情况下所有 IAtlasSystem 实现被注册为 Singleton，
///     使用此属性可以改为 Scoped 或 Transient。
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ServiceLifetimeAttribute : Attribute
{
    /// <summary>
    ///     初始化 Atlas 生命周期属性
    /// </summary>
    /// <param name="lifetime">服务生命周期</param>
    public ServiceLifetimeAttribute(ServiceLifetime lifetime)
    {
        Lifetime = lifetime;
    }

    /// <summary>
    ///     服务生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; }
}
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Std.App.Server.Queue;
using Std.App.Server.Systems;
using WireAttribute = Core.DI.WireAttribute;
using MeServiceLifetime = Microsoft.Extensions.DependencyInjection.ServiceLifetime;

namespace Std.App.Server.Core;

/// <summary>
///     系统扫描器，扫描程序集中所有 IAtlasSystem 实现并注册到 DI 容器。
///     为每个系统创建工厂，填充 [Wire] 属性和内置属性。
/// </summary>
public static class SystemScanner
{
    /// <summary>
    ///     扫描指定程序集中的 IAtlasSystem 实现，注册到 DI 容器
    /// </summary>
    /// <param name="services">DI 服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    public static void ScanAndRegister(IServiceCollection services, params Assembly[] assemblies)
    {
        var systemTypes = new List<Type>();

        foreach (var assembly in assemblies)
        foreach (var type in assembly.GetTypes())
            if (type.IsClass && !type.IsAbstract && typeof(IAtlasSystem).IsAssignableFrom(type))
                systemTypes.Add(type);

        foreach (var systemType in systemTypes) RegisterSystem(services, systemType);
    }

    /// <summary>
    ///     注册单个系统类型到 DI 容器
    /// </summary>
    /// <param name="services">DI 服务集合</param>
    /// <param name="systemType">系统类型</param>
    private static void RegisterSystem(IServiceCollection services, Type systemType)
    {
        // 确定生命周期
        var lifetime = MeServiceLifetime.Singleton;
        var lifetimeAttr = systemType.GetCustomAttribute<AtlasLifetimeAttribute>();

        if (lifetimeAttr is not null) lifetime = lifetimeAttr.Lifetime;

        // 创建工厂
        var descriptor = new ServiceDescriptor(systemType, provider =>
        {
            // 使用 ActivatorUtilities 创建实例
            var instance = ActivatorUtilities.CreateInstance(provider, systemType);

            // 填充 [Wire] 属性
            foreach (var prop in systemType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                if (prop.GetCustomAttribute<WireAttribute>() is not null && prop.CanWrite)
                {
                    var service = provider.GetService(prop.PropertyType);

                    if (service is not null) prop.SetValue(instance, service);
                }

            // 填充 AtlasSystem 内置属性
            if (instance is AtlasSystem atlasSystem) FillBuiltInProperties(atlasSystem, provider, systemType);

            return instance;
        }, lifetime);

        services.Add(descriptor);
    }

    /// <summary>
    ///     填充 AtlasSystem 内置的横切服务属性
    /// </summary>
    /// <param name="system">系统实例</param>
    /// <param name="provider">服务提供者</param>
    /// <param name="systemType">系统类型</param>
    private static void FillBuiltInProperties(AtlasSystem system, IServiceProvider provider, Type systemType)
    {
        var systemName = systemType.Name;

        // Logger
        if (system.Logger is null)
        {
            var logger = provider.GetService<IAtlasSystemLogger>();

            if (logger is not null)
                system.Logger = logger;
            else
                system.Logger = new ConsoleAtlasSystemLogger(systemName);
        }

        // Cache
        if (system.Cache is null) system.Cache = provider.GetService<IAtlasCache>() ?? new MemoryAtlasCache();

        // Queue
        if (system.Queue is null)
        {
            var queueProvider = provider.GetService<IQueueProvider>();

            if (queueProvider is not null)
            {
                system.Queue = new AtlasQueueAdapter(queueProvider);
            }
            else
            {
                var atlasQueue = provider.GetService<IAtlasQueue>();
                system.Queue = atlasQueue ?? new NullAtlasQueue();
            }
        }

        // EventBus
        if (system.EventBus is null)
            system.EventBus = provider.GetService<IAtlasEventBus>() ?? new InMemoryAtlasEventBus();
    }
}
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Nyar.PackageRegistry;

/// <summary>
///     Legion 注册表的依赖注入扩展方法
/// </summary>
public static class LegionRegistryServiceExtensions
{
    /// <summary>
    ///     向服务容器注册单个注册表适配器
    /// </summary>
    /// <typeparam name="T">注册表类型，必须实现 IRegistry</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection add_legion_registry<T>(this IServiceCollection services)
        where T : class, IRegistry
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IRegistry, T>());
        return services;
    }

    /// <summary>
    ///     向服务容器注册单个注册表适配器实例
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="registry">注册表实例</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection add_legion_registry(this IServiceCollection services, IRegistry registry)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IRegistry), registry));
        return services;
    }

    /// <summary>
    ///     向服务容器注册注册表工厂函数
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="implementationFactory">工厂函数，接收 IServiceProvider 并返回 IRegistry 实例</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection add_legion_registry(this IServiceCollection services,
        Func<IServiceProvider, IRegistry> implementationFactory)
    {
        services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IRegistry), implementationFactory));
        return services;
    }

    /// <summary>
    ///     通过程序集扫描自动发现并注册所有 IRegistry 实现
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assembly">要扫描的程序集</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection add_registries_from_assembly(this IServiceCollection services, Assembly assembly)
    {
        var registryTypes = assembly.GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IRegistry).IsAssignableFrom(t));

        foreach (var type in registryTypes)
            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IRegistry), type));

        return services;
    }

    /// <summary>
    ///     注册所有内置的默认注册表适配器（通过程序集扫描加载）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="registryAssemblies">要扫描的注册表程序集列表</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection add_legion_default_registries(this IServiceCollection services,
        params Assembly[] registryAssemblies)
    {
        if (registryAssemblies.Length == 0) registryAssemblies = discover_registry_assemblies();

        foreach (var assembly in registryAssemblies) services.add_registries_from_assembly(assembly);

        return services;
    }

    /// <summary>
    ///     注册 IRegistryResolver 服务，使其可用于构造函数注入
    ///     需要在注册所有 IRegistry 实现之后调用
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合，支持链式调用</returns>
    public static IServiceCollection add_registry_resolver(this IServiceCollection services)
    {
        services.TryAddSingleton<IRegistryResolver, RegistryResolver>();
        return services;
    }

    /// <summary>
    ///     从 DI 容器中获取所有已注册的注册表
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    /// <returns>注册表名称到实例的映射</returns>
    public static Dictionary<string, IRegistry> get_legion_registries(this IServiceProvider serviceProvider)
    {
        var registries = serviceProvider.GetServices<IRegistry>();
        var result = new Dictionary<string, IRegistry>(StringComparer.OrdinalIgnoreCase);

        foreach (var registry in registries) result[registry.name] = registry;

        return result;
    }

    /// <summary>
    ///     从 DI 容器中获取指定名称的注册表
    /// </summary>
    /// <param name="serviceProvider">服务提供器</param>
    /// <param name="registryName">注册表名称</param>
    /// <returns>注册表实例，若未找到则返回 null</returns>
    public static IRegistry? get_legion_registry(this IServiceProvider serviceProvider, string registryName)
    {
        var registries = serviceProvider.GetServices<IRegistry>();
        return registries.FirstOrDefault(r =>
            string.Equals(r.name, registryName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     自动发现当前 AppDomain 中已加载的注册表程序集
    /// </summary>
    private static Assembly[] discover_registry_assemblies()
    {
        return
        [
            .. AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name
                    ?.StartsWith("Nyar.PackageManager.Registry.", StringComparison.OrdinalIgnoreCase) == true)
        ];
    }
}
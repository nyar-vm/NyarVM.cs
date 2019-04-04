using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Std.Config;

/// <summary>
///     <see cref="IServiceCollection" /> 的 Sonic.Standard.Config 扩展方法。
/// </summary>
public static class SonicConfigServiceCollectionExtensions
{
    /// <summary>
    ///     将配置类型注册为 Singleton 服务，同时注�?<see cref="IOptions{T}" />�?see cref="IOptionsSnapshot{T}"/>�?see
    ///     cref="IOptionsMonitor{T}"/>�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     配置类型�?/typeparam>
    ///     <param name="services">
    ///         服务集合�?/param>
    ///         <param name="config">
    ///             配置实例�?/param>
    ///             <returns>服务集合，支持链式调用�?/returns>
    public static IServiceCollection add_sonic_config<T>(this IServiceCollection services, T config)
        where T : class, IConfigurable
    {
        services.AddSingleton(config);
        services.AddSingleton<IOptions<T>>(new OptionsWrapper<T>(config));
        services.AddSingleton<IOptionsSnapshot<T>>(sp => new OptionsSnapshot<T>(
            sp.GetRequiredService<IOptionsMonitor<T>>()));
        services.AddSingleton<IOptionsMonitor<T>>(sp =>
            new OptionsMonitorFromConfig<T>(config));
        return services;
    }

    /// <summary>
    ///     将配置类型注册为 Singleton 服务，支持热加载�?    /// 当配置文件变化时自动重建并更新所有注入点�?    ///
    /// </summary>
    /// <typeparam name="T">
    ///     配置类型�?/typeparam>
    ///     <param name="services">
    ///         服务集合�?/param>
    ///         <param name="watcher">
    ///             配置热加载监视器�?/param>
    ///             <returns>服务集合，支持链式调用�?/returns>
    public static IServiceCollection add_sonic_config<T>(this IServiceCollection services, IConfigWatcher<T> watcher)
        where T : class, IConfigurable
    {
        services.AddSingleton(watcher);
        services.AddSingleton<T>(sp => sp.GetRequiredService<IConfigWatcher<T>>().current);
        services.AddSingleton<IOptions<T>>(sp =>
            new OptionsWrapper<T>(sp.GetRequiredService<IConfigWatcher<T>>().current));
        services.AddSingleton<IOptionsMonitor<T>>(sp =>
            new OptionsMonitorFromWatcher<T>(sp.GetRequiredService<IConfigWatcher<T>>()));
        services.AddSingleton<IOptionsSnapshot<T>>(sp =>
            new OptionsSnapshot<T>(sp.GetRequiredService<IOptionsMonitor<T>>()));
        return services;
    }
}
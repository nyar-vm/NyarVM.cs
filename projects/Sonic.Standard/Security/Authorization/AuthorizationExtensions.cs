using Microsoft.Extensions.DependencyInjection;
using Std.Security.Authorization.Handlers;
using Std.Security.Authorization.Services;

namespace Std.Security.Authorization;

/// <summary>
///     授权服务注册扩展方法。
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    ///     注册 Sonic.Standard 授权服务及其默认处理器。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection add_sonic_authorization(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationService, AuthorizationService>();

        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, ResourceActionHandler>();
        services.AddSingleton<IAuthorizationHandler, RoleHandler>();

        return services;
    }

    /// <summary>
    ///     注册 Sonic.Standard 授权服务，使用自定义授权服务实现。
    /// </summary>
    /// <typeparam name="T">自定义授权服务类型</typeparam>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection add_sonic_authorization<T>(this IServiceCollection services)
        where T : class, IAuthorizationService
    {
        services.AddSingleton<IAuthorizationService, T>();

        services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationHandler, ResourceActionHandler>();
        services.AddSingleton<IAuthorizationHandler, RoleHandler>();

        return services;
    }
}
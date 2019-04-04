using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Std.App.Server.Core;
using Std.App.Server.Middleware;
using Std.App.Server.Queue;
using Std.App.Server.Systems;
using Std.Security;

namespace Std.App.Server.DI;

/// <summary>
///     Atlas 依赖注入扩展方法
/// </summary>
public static class AtlasServiceCollection
{
    /// <summary>
    ///     注册 Atlas 框架的基础服务（不含系统扫描）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection add_atlas(this IServiceCollection services)
    {
        services.TryAddTransient<JwtHandler>();

        RegisterDefaultServices(services);

        return services;
    }

    /// <summary>
    ///     注册 Atlas 核心服务，并扫描指定程序集中的 IAtlasSystem 实现
    /// </summary>
    /// <param name="services">DI 服务集合</param>
    /// <param name="assemblies">要扫描的程序集</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection add_atlas(this IServiceCollection services, params Assembly[] assemblies)
    {
        services.TryAddTransient<JwtHandler>();

        RegisterDefaultServices(services);

        if (assemblies.Length > 0) SystemScanner.ScanAndRegister(services, assemblies);

        return services;
    }

    /// <summary>
    ///     注册 Atlas 默认服务（如果尚未注册）
    /// </summary>
    /// <param name="services">DI 服务集合</param>
    private static void RegisterDefaultServices(IServiceCollection services)
    {
        services.TryAddSingleton<IAtlasSystemLogger, NullAtlasSystemLogger>();
        services.TryAddSingleton<IAtlasCache, MemoryAtlasCache>();
        services.TryAddSingleton<IAtlasQueue, NullAtlasQueue>();
        services.TryAddSingleton<IAtlasEventBus, InMemoryAtlasEventBus>();
    }

    /// <summary>
    ///     注册 JWT 认证中间件
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="options">JWT 验证选项</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection add_atlas_auth(this IServiceCollection services,
        JwtValidationOptions? options = null)
    {
        services.TryAddSingleton(new AuthMiddleware(options));
        return services;
    }

    /// <summary>
    ///     注册 CORS 中间件，委托 <see cref="Sonic.Net.Middleware.CorsMiddleware" /> 实现
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="allowedOrigins">允许的来源</param>
    /// <param name="allowedMethods">允许的方法</param>
    /// <param name="allowedHeaders">允许的请求头</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection add_atlas_cors(
        this IServiceCollection services,
        string[]? allowedOrigins = null,
        string[]? allowedMethods = null,
        string[]? allowedHeaders = null)
    {
        var options = new CorsOptions();

        if (allowedOrigins is { Length: > 0 })
        {
            options.allow_any_origin = false;
            options.allowed_origins = [.. allowedOrigins];
        }

        if (allowedMethods is { Length: > 0 }) options.allowed_methods = [.. allowedMethods];

        if (allowedHeaders is { Length: > 0 }) options.allowed_headers = [.. allowedHeaders];

        services.TryAddSingleton(new CorsMiddleware(options));
        return services;
    }

    /// <summary>
    ///     注册请求日志中间件，委托 <see cref="Sonic.Net.Middleware.RequestLoggingMiddleware" /> 实现
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection add_atlas_logging(this IServiceCollection services)
    {
        services.TryAddSingleton(new RequestLoggingMiddleware());
        return services;
    }

    /// <summary>
    ///     注册全局异常处理中间件，委托 <see cref="Sonic.Net.Middleware.ExceptionHandlingMiddleware" /> 实现
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="includeDetails">是否包含异常详情</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection add_atlas_exception_handler(this IServiceCollection services,
        bool includeDetails = false)
    {
        var options = new ExceptionHandlingOptions
        {
            include_exception_details = includeDetails
        };
        services.TryAddSingleton(new ExceptionHandlingMiddleware(options));
        return services;
    }

    /// <summary>
    ///     注册内存队列提供器，用于异步消息入队与消费
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection add_atlas_queue(this IServiceCollection services)
    {
        services.TryAddSingleton<IQueueProvider, MemoryQueueProvider>();
        return services;
    }

    /// <summary>
    ///     注册 Swagger 文档支持（尚未实现）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    /// <exception cref="NotImplementedException">Swagger 支持将在后续版本中实现</exception>
    public static IServiceCollection add_atlas_swagger(this IServiceCollection services)
    {
        throw new NotImplementedException("Swagger 支持将在后续版本中实现");
    }

    /// <summary>
    ///     注册请求验证支持（尚未实现）
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    /// <exception cref="NotImplementedException">验证支持将在后续版本中实现</exception>
    public static IServiceCollection add_atlas_validation(this IServiceCollection services)
    {
        throw new NotImplementedException("验证支持将在后续版本中实现");
    }
}
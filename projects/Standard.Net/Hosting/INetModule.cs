using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Std.Net.Hosting;

/// <summary>
///     托管服务接口，定义后台服务的启动和停止生命周期。
///     替代 <c>Microsoft.Extensions.Hosting.IHostedService</c>，避免引入额外依赖。
/// </summary>
public interface IHostedService
{
    /// <summary>
    ///     启动托管服务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task start(CancellationToken cancellationToken);

    /// <summary>
    ///     停止托管服务。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    Task stop(CancellationToken cancellationToken);
}

/// <summary>
///     Net 模块接口，每个模块自描述服务的注册逻辑和中间件配置。
///     实现此接口的类型会在应用启动时被自动发现并执行 <see cref="configure" />。
/// </summary>
public interface INetModule
{
    /// <summary>
    ///     模块名称，用于日志和诊断。
    /// </summary>
    string name { get; }

    /// <summary>
    ///     配置模块的服务注册和中间件管道。
    /// </summary>
    /// <param name="context">模块配置上下文，提供服务和中间件注册能力。</param>
    void configure(NetModuleContext context);
}

/// <summary>
///     模块配置上下文，提供 DI 容器和中间件管道的注册能力。
/// </summary>
public sealed class NetModuleContext
{
    /// <summary>
    ///     依赖注入服务集合。
    /// </summary>
    public IServiceCollection services { get; init; } = null!;

    /// <summary>
    ///     应用配置。
    /// </summary>
    public IConfiguration configuration { get; init; } = null!;
}
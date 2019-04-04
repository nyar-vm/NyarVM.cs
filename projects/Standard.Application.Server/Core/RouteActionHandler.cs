using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Std.App.Server.Core;

/// <summary>
///     将控制器 Action 方法适配为 <see cref="IRouteHandler" />，
///     负责参数绑定、方法调用和结果写入
/// </summary>
internal sealed class RouteActionHandler : IRouteHandler
{
    private readonly Type _controller_type;
    private readonly MethodInfo _method;
    private readonly IServiceProvider _services;

    /// <summary>
    ///     初始化 Action 处理器
    /// </summary>
    /// <param name="controllerType">控制器类型</param>
    /// <param name="method">Action 方法</param>
    /// <param name="services">服务提供者</param>
    public RouteActionHandler(Type controllerType, MethodInfo method, IServiceProvider services)
    {
        _controller_type = controllerType;
        _method = method;
        _services = services;
    }

    /// <summary>
    ///     处理请求：创建控制器实例、绑定参数、调用 Action、写入结果
    /// </summary>
    /// <param name="context">路由上下文</param>
    public async ValueTask handle(RouteContext context)
    {
        var controller = create_controller();
        var parameters = _method.GetParameters();
        var args = ParameterBinder.bind_parameters(parameters, context);

        object? result;

        try
        {
            result = _method.Invoke(controller, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            result = AtlasResult.internal_error(ex.InnerException.Message);
        }
        catch (Exception ex)
        {
            result = AtlasResult.internal_error(ex.Message);
        }

        if (result is Task task)
        {
            await task;

            var taskType = task.GetType();

            if (taskType.IsGenericType)
            {
                var resultProperty = taskType.GetProperty("Result");
                result = resultProperty?.GetValue(task);
            }
            else
            {
                result = AtlasResult.ok();
            }
        }

        if (result is ValueTask valueTask)
        {
            await valueTask;
            result = AtlasResult.ok();
        }

        apply_result(result, context);
    }

    /// <summary>
    ///     通过 DI 容器创建控制器实例
    /// </summary>
    private object create_controller()
    {
        return ActivatorUtilities.CreateInstance(_services, _controller_type);
    }

    /// <summary>
    ///     将 Action 返回值写入响应
    /// </summary>
    /// <param name="result">Action 返回值</param>
    /// <param name="context">路由上下文</param>
    private static void apply_result(object? result, RouteContext context)
    {
        switch (result)
        {
            case null:
                context.response.status_code = HttpStatusCode.ok;
                break;

            case AtlasResult atlasResult:
                atlasResult.apply_to(context);
                break;

            case string text:
                context.response.status_code = HttpStatusCode.ok;
                context.response.text(text);
                break;

            default:
                var json = JsonSerializer.Serialize(result);
                context.response.status_code = HttpStatusCode.ok;
                context.response.set_header("Content-Type", "application/json; charset=utf-8");
                context.response.text(json);
                break;
        }
    }
}
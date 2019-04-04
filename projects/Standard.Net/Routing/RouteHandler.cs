using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Std.Net.Http;

namespace Std.Net.Routing;

/// <summary>
///     路由处理器实现，通过反射调用控制器方法并处理参数绑定。
///     支持控制器基类注入、返回值自动序列化和依赖注入。
/// </summary>
public sealed class RouteHandler : IRouteHandler
{
    private readonly MethodInfo _method;
    private readonly IServiceProvider? _service_provider;
    private readonly string _template;

    /// <summary>
    ///     初始化路由处理器。
    /// </summary>
    /// <param name="method">目标方法</param>
    /// <param name="template">路由模板</param>
    /// <param name="serviceProvider">可选的服务提供器，用于依赖注入</param>
    public RouteHandler(MethodInfo method, string template, IServiceProvider? serviceProvider = null)
    {
        _method = method;
        _template = template;
        _service_provider = serviceProvider;
    }

    /// <inheritdoc />
    public async ValueTask handle(RouteContext context)
    {
        var controller = create_controller(context);

        var methodParams = _method.GetParameters();
        var args = new object?[methodParams.Length];

        for (var i = 0; i < methodParams.Length; i++) args[i] = await ParameterBinder.bind(methodParams[i], context);

        if ((int)context.response.status_code >= 400) return;

        var result = _method.Invoke(controller, args);

        await handle_result(result, context);
    }

    /// <summary>
    ///     创建控制器实例，支持依赖注入和基类属性注入。
    /// </summary>
    private object create_controller(RouteContext context)
    {
        object controller;

        if (_service_provider != null)
            try
            {
                controller = ActivatorUtilities.CreateInstance(_service_provider, _method.DeclaringType!);
            }
            catch
            {
                controller = Activator.CreateInstance(_method.DeclaringType!)!;
            }
        else
            controller = Activator.CreateInstance(_method.DeclaringType!)!;

        if (controller is ControllerBase baseController) baseController.context = context;

        return controller;
    }

    /// <summary>
    ///     处理控制器方法的返回值，支持自动 JSON 序列化。
    /// </summary>
    private static async ValueTask handle_result(object? result, RouteContext context)
    {
        if (result == null)
        {
            set_default_response(context);
            return;
        }

        var resultType = result.GetType();

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            var task = (Task)result;
            await task;

            var resultProperty = resultType.GetProperty("Result");
            var innerResult = resultProperty?.GetValue(result);
            serialize_result_if_needed(innerResult, context);
        }
        else if (result is Task task)
        {
            await task;
            set_default_response(context);
        }
        else
        {
            serialize_result_if_needed(result, context);
        }
    }

    /// <summary>
    ///     如果响应尚未写入，将返回值序列化为 JSON。
    /// </summary>
    private static void serialize_result_if_needed(object? result, RouteContext context)
    {
        if (context.response.body.Length > 0) return;

        if (result == null)
        {
            set_default_response(context);
            return;
        }

        if (result is IActionResult actionResult)
        {
            actionResult.execute(context);
            return;
        }

        var json = JsonSerializer.Serialize(result);
        context.response.status_code = HttpStatusCode.ok;
        context.response.json(json);
    }

    private static void set_default_response(RouteContext context)
    {
        if (context.response is { status_code: HttpStatusCode.ok, body.Length: 0 })
            context.response.status_code = HttpStatusCode.no_content;
    }
}

/// <summary>
///     操作结果接口，允许控制器方法返回自定义响应。
/// </summary>
public interface IActionResult
{
    /// <summary>
    ///     执行操作结果，写入 HTTP 响应。
    /// </summary>
    /// <param name="context">路由上下文</param>
    void execute(RouteContext context);
}

/// <summary>
///     JSON 操作结果，将数据序列化为 JSON 响应。
/// </summary>
public sealed class JsonResult : IActionResult
{
    private readonly object? _data;
    private readonly HttpStatusCode _status_code;

    /// <summary>
    ///     初始化 JSON 结果。
    /// </summary>
    /// <param name="data">要序列化的数据</param>
    /// <param name="statusCode">HTTP 状态码，默认 <see cref="HttpStatusCode.ok" /></param>
    public JsonResult(object? data, HttpStatusCode statusCode = HttpStatusCode.ok)
    {
        _data = data;
        _status_code = statusCode;
    }

    /// <inheritdoc />
    public void execute(RouteContext context)
    {
        var json = JsonSerializer.Serialize(_data);
        context.response.status_code = _status_code;
        context.response.json(json);
    }
}

/// <summary>
///     状态码操作结果，仅设置状态码不写入响应体。
/// </summary>
public sealed class StatusCodeResult : IActionResult
{
    private readonly HttpStatusCode _status_code;

    /// <summary>
    ///     初始化状态码结果。
    /// </summary>
    /// <param name="statusCode">HTTP 状态码</param>
    public StatusCodeResult(HttpStatusCode statusCode)
    {
        _status_code = statusCode;
    }

    /// <inheritdoc />
    public void execute(RouteContext context)
    {
        context.response.status_code = _status_code;
    }
}

/// <summary>
///     参数绑定器，从 HTTP 请求的不同部分绑定方法参数。
/// </summary>
public static class ParameterBinder
{
    /// <summary>
    ///     将 HTTP 请求中的值绑定到方法参数。
    /// </summary>
    /// <param name="param">方法参数信息</param>
    /// <param name="context">路由上下文</param>
    /// <returns>绑定后的参数值</returns>
    public static async Task<object?> bind(ParameterInfo param, RouteContext context)
    {
        var fromRoute = param.GetCustomAttribute<FromRouteAttribute>();
        if (fromRoute != null) return bind_from_route(param, context);

        var fromQuery = param.GetCustomAttribute<QueryAttribute>();
        if (fromQuery != null) return bind_from_query(param, context);

        var fromBody = param.GetCustomAttribute<BodyAttribute>();
        if (fromBody != null) return await bind_from_body(param, context);

        var fromHeader = param.GetCustomAttribute<HeaderAttribute>();
        if (fromHeader != null) return bind_from_header(param, context, fromHeader);

        if (is_complex_type(param.ParameterType)) return await bind_from_body(param, context);

        var routeValue = bind_from_route(param, context);
        if (routeValue != null) return routeValue;

        return bind_from_query(param, context);
    }

    private static object? bind_from_route(ParameterInfo param, RouteContext context)
    {
        if (context.request.route_params.TryGetValue(param.Name!, out var value))
            return convert_value(value, param.ParameterType);

        return param.HasDefaultValue ? param.DefaultValue : null;
    }

    private static object? bind_from_query(ParameterInfo param, RouteContext context)
    {
        var queryString = context.request.query_string;

        if (!string.IsNullOrEmpty(queryString))
        {
            var query = parse_query_string(queryString);

            if (query.TryGetValue(param.Name!, out var value)) return convert_value(value, param.ParameterType);
        }

        return param.HasDefaultValue ? param.DefaultValue : null;
    }

    private static async Task<object?> bind_from_body(ParameterInfo param, RouteContext context)
    {
        if (context.request.body.Length > 0)
        {
            var bodyText = context.request.body_as_text();

            try
            {
                var obj = JsonSerializer.Deserialize(bodyText, param.ParameterType);

                if (obj != null)
                {
                    var errors = ModelValidator.validate(obj);
                    if (errors.Count > 0)
                    {
                        var errorResponse = new { errors };
                        var json = JsonSerializer.Serialize(errorResponse);

                        context.response.status_code = HttpStatusCode.unprocessable_entity;
                        context.response.json(json);

                        return null;
                    }
                }

                return obj;
            }
            catch (JsonException)
            {
                context.response.status_code = HttpStatusCode.bad_request;

                return null;
            }
        }

        return param.HasDefaultValue ? param.DefaultValue : null;
    }

    private static object? bind_from_header(ParameterInfo param, RouteContext context, HeaderAttribute attr)
    {
        var headerName = !string.IsNullOrEmpty(attr.name) ? attr.name : param.Name!;
        if (context.request.headers.TryGetValue(headerName, out var value))
            return convert_value(value, param.ParameterType);

        return param.HasDefaultValue ? param.DefaultValue : null;
    }

    /// <summary>
    ///     解析查询字符串为键值对字典。
    /// </summary>
    /// <param name="queryString">查询字符串（含前缀 <c>?</c>）</param>
    /// <returns>键值对字典</returns>
    internal static Dictionary<string, string> parse_query_string(string queryString)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(queryString)) return result;

        var span = queryString.AsSpan();

        if (span[0] == '?') span = span[1..];

        foreach (var range in span.Split('&'))
        {
            var segment = span[range];
            var eqIndex = segment.IndexOf('=');

            if (eqIndex > 0)
            {
                var key = Uri.UnescapeDataString(segment[..eqIndex].ToString());
                var value = Uri.UnescapeDataString(segment[(eqIndex + 1)..].ToString());
                result[key] = value;
            }
        }

        return result;
    }

    private static object? convert_value(object? value, Type targetType)
    {
        if (value == null) return null;

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            targetType = Nullable.GetUnderlyingType(targetType)!;

        var strValue = value.ToString();

        if (targetType == typeof(string)) return strValue;

        if (targetType == typeof(int)) return int.TryParse(strValue, out var v) ? v : null;

        if (targetType == typeof(long)) return long.TryParse(strValue, out var v) ? v : null;

        if (targetType == typeof(bool)) return bool.TryParse(strValue, out var v) ? v : null;

        if (targetType == typeof(double)) return double.TryParse(strValue, out var v) ? v : null;

        if (targetType == typeof(Guid)) return Guid.TryParse(strValue, out var v) ? v : null;

        return Convert.ChangeType(strValue, targetType);
    }

    private static bool is_complex_type(Type type)
    {
        if (type.IsPrimitive || type == typeof(string)
                             || type == typeof(decimal) || type == typeof(DateTime)
                             || type == typeof(DateTimeOffset) || type == typeof(Guid)
                             || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
            return false;

        return true;
    }
}
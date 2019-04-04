using System.Globalization;
using System.Reflection;
using System.Text.Json;

namespace Std.App.Server.Core;

/// <summary>
///     参数绑定器，负责根据 <see cref="Sonic.Net.Http" /> 特性标记将 HTTP 请求数据绑定到 Action 方法参数
///     支持 FromRoute、Query、Body 以及无标记参数的自动推断
/// </summary>
public static class ParameterBinder
{
    /// <summary>
    ///     绑定 Action 方法的所有参数
    /// </summary>
    /// <param name="parameters">方法参数元数据集合</param>
    /// <param name="context">路由上下文</param>
    /// <returns>绑定后的参数值数组</returns>
    public static object?[] bind_parameters(ParameterInfo[] parameters, RouteContext context)
    {
        var values = new object?[parameters.Length];

        for (var i = 0; i < parameters.Length; i++) values[i] = bind_single(parameters[i], context);

        return values;
    }

    /// <summary>
    ///     绑定单个参数
    /// </summary>
    /// <param name="param">参数元数据</param>
    /// <param name="context">路由上下文</param>
    /// <returns>绑定后的值</returns>
    private static object? bind_single(ParameterInfo param, RouteContext context)
    {
        var paramType = param.ParameterType;

        if (paramType == typeof(HttpRequest)) return context.request;

        if (paramType == typeof(HttpResponse)) return context.response;

        if (paramType == typeof(RouteContext)) return context;

        if (param.IsDefined(typeof(BodyAttribute), true)) return bind_from_body(param, context);

        if (param.IsDefined(typeof(FromRouteAttribute), true))
        {
            var attr = param.GetCustomAttribute<FromRouteAttribute>()!;
            var name = attr.name ?? param.Name ?? string.Empty;
            return bind_from_route(param, context, name);
        }

        if (param.IsDefined(typeof(QueryAttribute), true))
        {
            var attr = param.GetCustomAttribute<QueryAttribute>()!;
            var name = attr.name ?? param.Name ?? string.Empty;
            return bind_from_query(param, context, name);
        }

        var paramName = param.Name ?? string.Empty;

        if (context.request.route_params.TryGetValue(paramName, out var routeValue))
            return convert_to_type(routeValue, paramType);

        var queryValue = try_get_query_param(context, paramName);

        if (queryValue is not null) return convert_to_type(queryValue, paramType);

        if (paramType.IsClass && paramType != typeof(string)) return bind_from_body(param, context);

        return paramType.IsValueType ? Activator.CreateInstance(paramType) : null;
    }

    /// <summary>
    ///     从路由参数绑定
    /// </summary>
    private static object? bind_from_route(ParameterInfo param, RouteContext context, string name)
    {
        if (context.request.route_params.TryGetValue(name, out var value))
            return convert_to_type(value, param.ParameterType);

        return param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
    }

    /// <summary>
    ///     从查询字符串绑定
    /// </summary>
    private static object? bind_from_query(ParameterInfo param, RouteContext context, string name)
    {
        var value = try_get_query_param(context, name);

        if (value is not null) return convert_to_type(value, param.ParameterType);

        return param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
    }

    /// <summary>
    ///     从请求体绑定，自动反序列化 JSON
    /// </summary>
    private static object? bind_from_body(ParameterInfo param, RouteContext context)
    {
        var body = context.request.body;

        if (body is null || body.Length == 0)
            return param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;

        try
        {
            return JsonSerializer.Deserialize(body, param.ParameterType);
        }
        catch
        {
            return param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null;
        }
    }

    /// <summary>
    ///     从查询字符串中解析指定键的值
    /// </summary>
    private static string? try_get_query_param(RouteContext context, string name)
    {
        var qs = context.request.query_string;

        if (string.IsNullOrEmpty(qs)) return null;

        var q = qs.TrimStart('?');
        var pairs = q.Split('&');

        foreach (var pair in pairs)
        {
            var eq = pair.IndexOf('=');

            if (eq < 0) continue;

            var key = Uri.UnescapeDataString(pair[..eq]);

            if (string.Equals(key, name, StringComparison.Ordinal)) return Uri.UnescapeDataString(pair[(eq + 1)..]);
        }

        return null;
    }

    /// <summary>
    ///     将字符串值转换为指定类型，支持基本类型的转换
    /// </summary>
    /// <param name="value">字符串值</param>
    /// <param name="targetType">目标类型</param>
    /// <returns>转换后的值</returns>
    private static object? convert_to_type(string value, Type targetType)
    {
        if (targetType == typeof(string)) return value;

        if (targetType == typeof(int))
            return int.TryParse(value, CultureInfo.InvariantCulture, out var intResult) ? intResult : 0;

        if (targetType == typeof(long))
            return long.TryParse(value, CultureInfo.InvariantCulture, out var longResult) ? longResult : 0L;

        if (targetType == typeof(bool)) return bool.TryParse(value, out var boolResult) && boolResult;

        if (targetType == typeof(double))
            return double.TryParse(value, CultureInfo.InvariantCulture, out var doubleResult) ? doubleResult : 0.0;

        if (targetType == typeof(float))
            return float.TryParse(value, CultureInfo.InvariantCulture, out var floatResult) ? floatResult : 0.0f;

        if (targetType == typeof(Guid)) return Guid.TryParse(value, out var guidResult) ? guidResult : Guid.Empty;

        if (targetType == typeof(DateTime))
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind,
                out var dtResult)
                ? dtResult
                : DateTime.MinValue;

        if (targetType.IsEnum)
            return Enum.TryParse(targetType, value, true, out var enumResult)
                ? enumResult
                : Activator.CreateInstance(targetType);

        return null;
    }
}
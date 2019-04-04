using Std.Category;
using Std.Net.Http;
using HttpMethod = Std.Net.Http.HttpMethod;

namespace Std.Net;

/// <summary>
///     URL 路由器，将请求路径和方法匹配到注册的路由处理器。
///     支持三种路径参数语法：<c>:param</c>、<c>{name}</c> 和 <c>{name:type}</c>（类型约束），以及精确匹配。
/// </summary>
public sealed class Router
{
    private readonly List<RouteEntry> _routes = [];

    /// <summary>
    ///     注册 GET 路由。
    /// </summary>
    /// <param name="pattern">URL 模式，如 <c>/users/:id</c> 或 <c>/users/{id:int}</c></param>
    /// <param name="handler">路由处理器</param>
    public void get(string pattern, IRouteHandler handler)
    {
        _routes.Add(new RouteEntry(HttpMethod.get, pattern, handler));
    }

    /// <summary>
    ///     注册 POST 路由。
    /// </summary>
    /// <param name="pattern">URL 模式</param>
    /// <param name="handler">路由处理器</param>
    public void post(string pattern, IRouteHandler handler)
    {
        _routes.Add(new RouteEntry(HttpMethod.post, pattern, handler));
    }

    /// <summary>
    ///     注册 PUT 路由。
    /// </summary>
    /// <param name="pattern">URL 模式</param>
    /// <param name="handler">路由处理器</param>
    public void put(string pattern, IRouteHandler handler)
    {
        _routes.Add(new RouteEntry(HttpMethod.put, pattern, handler));
    }

    /// <summary>
    ///     注册 DELETE 路由。
    /// </summary>
    /// <param name="pattern">URL 模式</param>
    /// <param name="handler">路由处理器</param>
    public void delete(string pattern, IRouteHandler handler)
    {
        _routes.Add(new RouteEntry(HttpMethod.delete, pattern, handler));
    }

    /// <summary>
    ///     注册 PATCH 路由。
    /// </summary>
    /// <param name="pattern">URL 模式</param>
    /// <param name="handler">路由处理器</param>
    public void patch(string pattern, IRouteHandler handler)
    {
        _routes.Add(new RouteEntry(HttpMethod.patch, pattern, handler));
    }

    /// <summary>
    ///     尝试匹配请求到路由处理器。
    /// </summary>
    /// <param name="request">HTTP 请求</param>
    /// <returns>
    ///     匹配成功返回 <c>Some(handler)</c>，否则返回 <c>None</c>。
    ///     匹配成功时，路径参数已填充到 <see cref="HttpRequest.route_params" /> 中。
    /// </returns>
    public Option<IRouteHandler> match(HttpRequest request)
    {
        foreach (var route in _routes)
        {
            if (route.method != request.method) continue;

            if (try_match_pattern(route.pattern, request.path, request.route_params))
                return Option<IRouteHandler>.some(route.handler);
        }

        return Option<IRouteHandler>.none;
    }

    /// <summary>
    ///     尝试将路径模式与实际路径匹配。
    ///     支持三种参数语法：<c>:param</c>、<c>{name}</c>（字符串参数）和 <c>{name:type}</c>（类型约束参数）。
    ///     类型约束包括：<c>int</c>、<c>long</c>、<c>bool</c>、<c>double</c>、<c>guid</c>、<c>alpha</c>。
    /// </summary>
    private static bool try_match_pattern(string pattern, string path, Dictionary<string, string> parameters)
    {
        var patternSegments = pattern.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathSegments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (patternSegments.Length != pathSegments.Length) return false;

        for (var i = 0; i < patternSegments.Length; i++)
        {
            var segment = patternSegments[i];
            var pathValue = Uri.UnescapeDataString(pathSegments[i]);

            if (segment.StartsWith(':'))
            {
                var paramName = segment[1..];
                parameters[paramName] = pathValue;
                continue;
            }

            if (segment.StartsWith('{') && segment.EndsWith('}'))
            {
                var inner = segment[1..^1];

                if (try_parse_parameter_segment(inner, pathValue, parameters)) continue;

                return false;
            }

            if (segment != pathSegments[i]) return false;
        }

        return true;
    }

    /// <summary>
    ///     解析花括号参数段，如 <c>id</c> 或 <c>id:int</c>。
    ///     如果存在类型约束，验证路径值是否符合约束。
    /// </summary>
    private static bool try_parse_parameter_segment(string inner, string pathValue,
        Dictionary<string, string> parameters)
    {
        var colonIndex = inner.IndexOf(':');

        if (colonIndex < 0)
        {
            parameters[inner] = pathValue;
            return true;
        }

        var paramName = inner[..colonIndex];
        var constraint = inner[(colonIndex + 1)..];

        if (!validate_constraint(constraint, pathValue)) return false;

        parameters[paramName] = pathValue;
        return true;
    }

    /// <summary>
    ///     验证路径值是否符合类型约束。
    /// </summary>
    /// <param name="constraint">类型约束名称</param>
    /// <param name="value">路径值</param>
    /// <returns>符合约束返回 <c>true</c>，否则返回 <c>false</c></returns>
    private static bool validate_constraint(string constraint, string value)
    {
        return constraint switch
        {
            "int" => int.TryParse(value, out _),
            "long" => long.TryParse(value, out _),
            "bool" => bool.TryParse(value, out _),
            "double" => double.TryParse(value, out _),
            "guid" => Guid.TryParse(value, out _),
            "alpha" => !string.IsNullOrEmpty(value) && value.All(char.IsLetter),
            _ => true
        };
    }

    private sealed class RouteEntry
    {
        public RouteEntry(HttpMethod method, string pattern, IRouteHandler handler)
        {
            this.method = method;
            this.pattern = pattern;
            this.handler = handler;
        }

        public HttpMethod method { get; }
        public string pattern { get; }
        public IRouteHandler handler { get; }
    }
}
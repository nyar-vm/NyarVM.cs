using System.Reflection;
using Std.App.Server.Attributes;
using HttpMethod = Sonic.Net.HttpMethod;

namespace Std.App.Server.Core;

/// <summary>
///     控制器扫描器，发现所有实现 <see cref="IController" /> 的类型，
///     读取其 <see cref="Sonic.Net.Http" /> 命名空间下的特性标记并注册到路由器中
/// </summary>
public static class ControllerScanner
{
    /// <summary>
    ///     扫描指定程序集中的控制器类型并注册到路由器
    /// </summary>
    /// <param name="router">目标路由器</param>
    /// <param name="assembly">要扫描的程序集</param>
    /// <param name="services">服务提供者，用于创建控制器实例</param>
    public static void scan_and_register(Router router, Assembly assembly, IServiceProvider services)
    {
        var controllerTypes = assembly.GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .Where(t => typeof(IController).IsAssignableFrom(t))
            .ToList();

        foreach (var controllerType in controllerTypes) register_controller(router, controllerType, services);
    }

    /// <summary>
    ///     注册单个控制器的所有 Action
    /// </summary>
    /// <param name="router">目标路由器</param>
    /// <param name="controllerType">控制器类型</param>
    /// <param name="services">服务提供者</param>
    private static void register_controller(Router router, Type controllerType, IServiceProvider services)
    {
        var routePrefix = controllerType.GetCustomAttribute<RoutePrefixAttribute>();
        var basePath = routePrefix?.prefix ?? string.Empty;

        var methods =
            controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var method in methods)
        {
            var httpMethodInfo = get_http_method_info(method);

            if (httpMethodInfo is null) continue;

            var fullTemplate = combine_path(basePath, httpMethodInfo.Value.template);
            var handler = new RouteActionHandler(controllerType, method, services);

            register_route(router, httpMethodInfo.Value.method, fullTemplate, handler);
        }
    }

    /// <summary>
    ///     从方法上提取 HTTP 方法和路由模板信息
    /// </summary>
    private static (HttpMethod method, string template)? get_http_method_info(MethodInfo methodInfo)
    {
        if (methodInfo.GetCustomAttribute<GetAttribute>() is { } get) return (HttpMethod.get, get.template);

        if (methodInfo.GetCustomAttribute<PostAttribute>() is { } post) return (HttpMethod.post, post.template);

        if (methodInfo.GetCustomAttribute<PutAttribute>() is { } put) return (HttpMethod.put, put.template);

        if (methodInfo.GetCustomAttribute<DeleteAttribute>() is { } del) return (HttpMethod.delete, del.template);

        if (methodInfo.GetCustomAttribute<PatchAttribute>() is { } patch) return (HttpMethod.patch, patch.template);

        return null;
    }

    /// <summary>
    ///     根据 HTTP 方法类型注册路由
    /// </summary>
    private static void register_route(Router router, HttpMethod method, string template, IRouteHandler handler)
    {
        if (method == HttpMethod.get)
            router.get(template, handler);
        else if (method == HttpMethod.post)
            router.post(template, handler);
        else if (method == HttpMethod.put)
            router.put(template, handler);
        else if (method == HttpMethod.delete)
            router.delete(template, handler);
        else if (method == HttpMethod.patch) router.patch(template, handler);
    }

    /// <summary>
    ///     组合路由前缀和模板路径
    /// </summary>
    /// <param name="basePath">控制器级别路由前缀</param>
    /// <param name="template">Action 级别路由模板</param>
    /// <returns>完整路由模板</returns>
    private static string combine_path(string basePath, string template)
    {
        if (string.IsNullOrEmpty(basePath)) return template;

        if (string.IsNullOrEmpty(template)) return basePath;

        var normalizedBase = basePath.TrimEnd('/');
        var normalizedTemplate = template.TrimStart('/');

        return $"{normalizedBase}/{normalizedTemplate}";
    }
}
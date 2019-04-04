namespace Std.Net.Http;

/// <summary>
///     标记方法为 HTTP 服务器路由，并指定路由模板
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RouteAttribute : Attribute
{
    /// <summary>
    ///     初始化 RouteAttribute 的新实例
    /// </summary>
    /// <param name="template">路由模板</param>
    public RouteAttribute(string template)
    {
        this.template = template;
    }

    /// <summary>
    ///     路由模板
    /// </summary>
    public string template { get; }
}
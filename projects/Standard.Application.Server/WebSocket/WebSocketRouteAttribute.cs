namespace Std.App.Server.WebSocket;

/// <summary>
///     定义 WebSocket 处理器的路由路径
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class WebSocketRouteAttribute : Attribute
{
    /// <summary>
    ///     初始化 WebSocket 路由特性
    /// </summary>
    /// <param name="route">路由路径</param>
    public WebSocketRouteAttribute(string route)
    {
        this.route = route;
    }

    /// <summary>
    ///     WebSocket 路由路径
    /// </summary>
    public string route { get; }
}
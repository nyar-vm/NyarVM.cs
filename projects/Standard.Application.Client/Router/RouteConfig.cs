namespace Std.App.Client;

/// <summary>
///     路由配置条目
/// </summary>
public sealed class RouteConfig
{
    /// <summary>
    ///     初始化路由配置
    /// </summary>
    /// <param name="path">路径模式，支持参数占位符如 <c>/users/:id</c></param>
    /// <param name="componentType">路由对应的组件类型</param>
    /// <param name="children">子路由</param>
    public RouteConfig(
        string path,
        Type componentType,
        IReadOnlyList<RouteConfig>? children = null)
    {
        Path = path;
        ComponentType = componentType;
        Children = children ?? [];
    }

    /// <summary>
    ///     路径模式
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     组件类型
    /// </summary>
    public Type ComponentType { get; }

    /// <summary>
    ///     子路由
    /// </summary>
    public IReadOnlyList<RouteConfig> Children { get; }
}
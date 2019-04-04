namespace VOA.ToolChain.Tests;

/// <summary>
///     VOA 路由器，管理一组路由规则并支持编译后的匹配
/// </summary>
public sealed record VoaRouter
{
    /// <summary>
    ///     路由规则列表
    /// </summary>
    public List<VoaRoute> Routes { get; init; } = [];

    /// <summary>
    ///     是否已编译
    /// </summary>
    public bool IsCompiled { get; init; } = false;
}

/// <summary>
///     VOA 路由规则，定义路径与组件的映射关系
/// </summary>
public sealed record VoaRoute
{
    /// <summary>
    ///     路由路径模式
    /// </summary>
    public string Path { get; init; } = "";

    /// <summary>
    ///     目标组件名称
    /// </summary>
    public string Component { get; init; } = "";

    /// <summary>
    ///     HTTP 方法，默认为 GET
    /// </summary>
    public string Method { get; init; } = "GET";
}

/// <summary>
///     VOA 路由匹配结果
/// </summary>
public sealed record VoaRouteMatch
{
    /// <summary>
    ///     是否找到匹配的路由
    /// </summary>
    public bool Found { get; init; } = false;

    /// <summary>
    ///     匹配到的路由规则，未找到时为 null
    /// </summary>
    public VoaRoute? Route { get; init; }

    /// <summary>
    ///     路径参数，键为参数名，值为参数值
    /// </summary>
    public Dictionary<string, string> Params { get; init; } = new();
}
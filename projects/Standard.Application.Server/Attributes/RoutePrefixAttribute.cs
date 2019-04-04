namespace Std.App.Server.Attributes;

/// <summary>
///     定义控制器的路由前缀，用于组合多个 Action 路由模板的基础路径
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RoutePrefixAttribute : Attribute
{
    /// <summary>
    ///     初始化路由前缀特性
    /// </summary>
    /// <param name="prefix">路由前缀模板</param>
    public RoutePrefixAttribute(string prefix)
    {
        this.prefix = prefix;
    }

    /// <summary>
    ///     路由前缀模板，如 <c>/api/users</c>
    /// </summary>
    public string prefix { get; }
}
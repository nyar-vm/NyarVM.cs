namespace Std.Net.Http;

/// <summary>
///     标记方法为 HTTP GET 请求，并指定路由模板
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class GetAttribute : Attribute
{
    /// <summary>
    ///     初始化 GetAttribute 的新实例
    /// </summary>
    /// <param name="template">路由模板</param>
    public GetAttribute(string template)
    {
        this.template = template;
    }

    /// <summary>
    ///     路由模板
    /// </summary>
    public string template { get; }
}
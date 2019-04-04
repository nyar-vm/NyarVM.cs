namespace Std.Net.Http;

/// <summary>
///     标记方法为 HTTP PUT 请求，并指定路由模板
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class PutAttribute : Attribute
{
    /// <summary>
    ///     初始化 PutAttribute 的新实例
    /// </summary>
    /// <param name="template">路由模板</param>
    public PutAttribute(string template)
    {
        this.template = template;
    }

    /// <summary>
    ///     路由模板
    /// </summary>
    public string template { get; }
}
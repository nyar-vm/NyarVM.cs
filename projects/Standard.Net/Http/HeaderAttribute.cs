namespace Std.Net.Http;

/// <summary>
///     标记 HTTP 请求的头部信息，可应用于方法或参数
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter)]
public sealed class HeaderAttribute : Attribute
{
    /// <summary>
    ///     初始化 HeaderAttribute 的新实例
    /// </summary>
    /// <param name="name">头部名称</param>
    /// <param name="value">头部值</param>
    public HeaderAttribute(string name, string value)
    {
        this.name = name;
        this.value = value;
    }

    /// <summary>
    ///     头部名称
    /// </summary>
    public string name { get; }

    /// <summary>
    ///     头部值
    /// </summary>
    public string value { get; }
}
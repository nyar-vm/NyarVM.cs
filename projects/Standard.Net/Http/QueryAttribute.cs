namespace Std.Net.Http;

/// <summary>
///     标记参数为 HTTP 查询字符串参数
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class QueryAttribute : Attribute
{
    /// <summary>
    ///     查询参数名称，为 null 时使用参数名
    /// </summary>
    public string? name { get; set; }
}
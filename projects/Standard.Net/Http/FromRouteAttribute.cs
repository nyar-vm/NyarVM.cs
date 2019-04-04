namespace Std.Net.Http;

/// <summary>
///     标记参数应从路由路径中绑定。
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class FromRouteAttribute : Attribute
{
    /// <summary>
    ///     路由参数名称，为 null 时使用参数名。
    /// </summary>
    public string? name { get; set; }
}
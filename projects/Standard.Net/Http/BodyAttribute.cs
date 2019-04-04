namespace Std.Net.Http;

/// <summary>
///     标记参数为 HTTP 请求体
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class BodyAttribute : Attribute
{
}
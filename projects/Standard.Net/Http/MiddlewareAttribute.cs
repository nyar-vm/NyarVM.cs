namespace Std.Net.Http;

/// <summary>
///     标记方法为 HTTP 中间件
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MiddlewareAttribute : Attribute
{
}
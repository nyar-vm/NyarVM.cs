using System;

namespace Core.Security.Authorization;

/// <summary>
///     标记一个方法为安全端点，启用认证保护
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SecureEndpointAttribute : Attribute
{
}
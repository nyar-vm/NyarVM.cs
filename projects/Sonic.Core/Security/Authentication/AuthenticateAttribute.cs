using System;

namespace Core.Security.Authentication;

/// <summary>
///     标记需要身份验证的类或方法
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthenticateAttribute : Attribute
{
}
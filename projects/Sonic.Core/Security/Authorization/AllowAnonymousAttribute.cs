using System;

namespace Core.Security.Authorization;

/// <summary>
///     标记允许匿名访问的类或方法
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowAnonymousAttribute : Attribute
{
}
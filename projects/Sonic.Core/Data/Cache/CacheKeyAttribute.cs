using System;

namespace Core.Data.Cache;

/// <summary>
///     标记参数为缓存键组成部分
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public sealed class CacheKeyAttribute : Attribute
{
}
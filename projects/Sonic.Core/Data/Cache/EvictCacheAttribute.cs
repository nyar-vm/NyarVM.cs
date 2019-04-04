using System;

namespace Core.Data.Cache;

/// <summary>
///     标记方法执行后清除缓存
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class EvictCacheAttribute : Attribute
{
    /// <summary>
    ///     要清除的缓存键，为 null 时清除所有缓存
    /// </summary>
    public string? key { get; set; }
}
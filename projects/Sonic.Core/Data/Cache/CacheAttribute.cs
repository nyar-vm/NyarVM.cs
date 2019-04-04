using System;

namespace Core.Data.Cache;

/// <summary>
///     标记方法启用缓存
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class CacheAttribute : Attribute
{
    /// <summary>
    ///     缓存持续时间（秒），默认为 300
    /// </summary>
    public int duration { get; set; } = 300;

    /// <summary>
    ///     缓存键，为 null 时自动生成
    /// </summary>
    public string? key { get; set; }
}
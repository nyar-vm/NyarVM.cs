using Core.Data.Cache;

namespace Std.Data.Cache;

/// <summary>
///     缓存上下文，提供全局缓存实例的访问入口。
/// </summary>
public static class CacheContext
{
    /// <summary>
    ///     全局内存缓存实例。
    /// </summary>
    public static IMemoryCache<string> memory_cache { get; set; } = new MemoryCache<string>();

    /// <summary>
    ///     清除所有缓存条目。
    /// </summary>
    public static void clear_all()
    {
        memory_cache = new MemoryCache<string>();
    }
}
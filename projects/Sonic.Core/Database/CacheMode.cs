namespace Core.Database;

/// <summary>
///     缓存写入模式
/// </summary>
public enum CacheMode
{
    /// <summary>
    ///     透写模式：每次写入同时更新缓存和底层存储
    /// </summary>
    WriteThrough,

    /// <summary>
    ///     回写模式：先写入缓存，异步批量刷入底层存储
    /// </summary>
    WriteBack
}
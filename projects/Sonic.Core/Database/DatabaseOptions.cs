namespace Core.Database;

/// <summary>
///     数据库通用配置基类
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    ///     数据库名称
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    ///     页面大小（字节）
    /// </summary>
    public int PageSize { get; init; } = 4096;

    /// <summary>
    ///     页缓存大小（页面数）
    /// </summary>
    public int CacheSize { get; init; } = 1024;
}
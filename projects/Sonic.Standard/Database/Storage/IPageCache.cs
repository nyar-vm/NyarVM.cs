namespace Std.Database.Storage;

/// <summary>
///     页面缓存接口
/// </summary>
internal interface IPageCache
{
    /// <summary>
    ///     页面大小
    /// </summary>
    int page_size { get; }

    /// <summary>
    ///     缓存容量
    /// </summary>
    int capacity { get; }

    /// <summary>
    ///     缓存命中次数
    /// </summary>
    long hit_count { get; }

    /// <summary>
    ///     缓存未命中次数
    /// </summary>
    long miss_count { get; }

    /// <summary>
    ///     获取页面
    /// </summary>
    ValueTask<Page> get_page(long pageId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     分配新页面
    /// </summary>
    ValueTask<Page> allocate_page(CancellationToken cancellationToken = default);

    /// <summary>
    ///     标记页面为脏页
    /// </summary>
    void mark_dirty(long pageId);

    /// <summary>
    ///     写入页面到缓存
    /// </summary>
    ValueTask put_page(long pageId, Page page, CancellationToken cancellationToken = default);

    /// <summary>
    ///     刷盘所有脏页
    /// </summary>
    ValueTask flush(CancellationToken cancellationToken = default);

    /// <summary>
    ///     尝试从缓存中获取页面（同步快速路径），避免分配异步状态机
    ///     仅在页面已缓存时返回 true，未缓存时不触发磁盘 I/O
    /// </summary>
    /// <param name="pageId">页面 ID</param>
    /// <param name="page">获取到的页面（仅在返回 true 时有效）</param>
    /// <returns>页面是否在缓存中</returns>
    bool try_get_page(long pageId, out Page page);

    /// <summary>
    ///     释放页面：从缓存中移除并归还到存储引擎的空闲页池
    /// </summary>
    /// <param name="pageId">页面 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask deallocate_page(long pageId, CancellationToken cancellationToken = default);
}
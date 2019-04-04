namespace Std.Database.Storage;

/// <summary>
///     存储引擎接口，抽象底层存储实现
/// </summary>
internal interface IStorageEngine : IDisposable, IAsyncDisposable
{
    /// <summary>
    ///     页面大小
    /// </summary>
    int page_size { get; }

    /// <summary>
    ///     当前最大页面 ID
    /// </summary>
    long max_page_id { get; }

    /// <summary>
    ///     读取页面
    /// </summary>
    /// <param name="pageId">页面 ID</param>
    /// <param name="buffer">目标缓冲区</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask read(long pageId, Memory<byte> buffer, CancellationToken cancellationToken = default);

    /// <summary>
    ///     写入页面
    /// </summary>
    /// <param name="pageId">页面 ID</param>
    /// <param name="data">页面数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask write(long pageId, ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>
    ///     分配新页面
    /// </summary>
    /// <returns>新页面 ID</returns>
    long allocate_page();

    /// <summary>
    ///     释放页面
    /// </summary>
    /// <param name="pageId">页面 ID</param>
    void free_page(long pageId);

    /// <summary>
    ///     同步刷盘
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    ValueTask flush(CancellationToken cancellationToken = default);
}
namespace Std.Database.Storage;

/// <summary>
///     基于文件的存储引擎实现
/// </summary>
internal sealed class FileStorageEngine : IStorageEngine
{
    private readonly string _base_path;
    private readonly FileStream _data_file;
    private readonly Stack<long> _free_pages;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;
    private long _max_page_id;

    /// <summary>
    ///     创建文件存储引擎
    /// </summary>
    /// <param name="basePath">数据目录</param>
    /// <param name="pageSize">页面大小</param>
    public FileStorageEngine(string basePath, int pageSize = 4096)
    {
        _base_path = basePath;
        page_size = pageSize;
        _free_pages = new Stack<long>();

        if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

        var dataPath = Path.Combine(basePath, "light.dat");
        _data_file = new FileStream(dataPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096,
            FileOptions.RandomAccess);

        _max_page_id = _data_file.Length / pageSize;
    }

    /// <inheritdoc />
    public int page_size { get; }

    /// <inheritdoc />
    public long max_page_id => _max_page_id;

    /// <inheritdoc />
    public async ValueTask read(long pageId, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _data_file.Seek(pageId * page_size, SeekOrigin.Begin);
            await _data_file.ReadExactlyAsync(buffer[..page_size], cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask write(long pageId, ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _data_file.Seek(pageId * page_size, SeekOrigin.Begin);
            await _data_file.WriteAsync(data[..page_size], cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public long allocate_page()
    {
        lock (_free_pages)
        {
            if (_free_pages.Count > 0) return _free_pages.Pop();
        }

        return Interlocked.Increment(ref _max_page_id) - 1;
    }

    /// <inheritdoc />
    public void free_page(long pageId)
    {
        lock (_free_pages)
        {
            _free_pages.Push(pageId);
        }
    }

    /// <inheritdoc />
    public async ValueTask flush(CancellationToken cancellationToken = default)
    {
        await _data_file.FlushAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        await _data_file.DisposeAsync();
        _lock.Dispose();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
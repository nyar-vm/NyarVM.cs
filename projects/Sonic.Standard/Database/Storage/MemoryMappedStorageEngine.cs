using System.IO.MemoryMappedFiles;

namespace Std.Database.Storage;

/// <summary>
///     基于内存映射文件的存储引擎，使用块拷贝优化读写性能
/// </summary>
internal sealed class MemoryMappedStorageEngine : IStorageEngine
{
    #region 构造函数

    /// <summary>
    ///     创建内存映射存储引擎
    /// </summary>
    /// <param name="basePath">数据目录</param>
    /// <param name="pageSize">页面大小</param>
    /// <param name="readOnly">是否只读</param>
    public MemoryMappedStorageEngine(string basePath, int pageSize = 4096, bool readOnly = false)
    {
        _base_path = basePath;
        page_size = pageSize;
        _read_only = readOnly;
        _free_pages = new Stack<long>();

        if (!Directory.Exists(basePath)) Directory.CreateDirectory(basePath);

        var dataPath = Path.Combine(basePath, "light.dat");
        var fileAccess = readOnly ? FileAccess.Read : FileAccess.ReadWrite;
        var fileShare = readOnly ? FileShare.Read : FileShare.None;
        var fileMode = readOnly ? FileMode.Open : FileMode.OpenOrCreate;

        _data_file = new FileStream(dataPath, fileMode, fileAccess, fileShare, 4096,
            FileOptions.RandomAccess);

        _max_page_id = _data_file.Length / pageSize;

        if (_data_file.Length > 0)
        {
            _mmap_file = MemoryMappedFile.CreateFromFile(_data_file, null, 0,
                readOnly ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None, readOnly);
            _view_accessor = _mmap_file.CreateViewAccessor();
        }
    }

    #endregion

    #region 读取

    /// <inheritdoc />
    public async ValueTask read(long pageId, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            ensure_mapped(pageId + 1);

            var position = pageId * page_size;
            var temp = new byte[page_size];
            _view_accessor!.ReadArray(position, temp, 0, page_size);
            temp.AsSpan().CopyTo(buffer.Span[..page_size]);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 写入

    /// <inheritdoc />
    public async ValueTask write(long pageId, ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_read_only) throw new InvalidOperationException("只读模式下不允许写入");

        await _lock.WaitAsync(cancellationToken);
        try
        {
            ensure_mapped(pageId + 1);

            var position = pageId * page_size;
            var temp = data.Span[..page_size].ToArray();
            _view_accessor!.WriteArray(position, temp, 0, page_size);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 刷盘

    /// <inheritdoc />
    public async ValueTask flush(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _view_accessor?.Flush();
            await _data_file.FlushAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 内部方法

    /// <summary>
    ///     确保内存映射覆盖所需页面数
    /// </summary>
    /// <param name="requiredPageCount">所需页面数</param>
    private void ensure_mapped(long requiredPageCount)
    {
        var requiredSize = requiredPageCount * page_size;
        var currentSize = _data_file.Length;

        if (requiredSize <= currentSize && _view_accessor is not null) return;

        if (requiredSize > currentSize)
        {
            _view_accessor?.Dispose();
            _mmap_file?.Dispose();

            _data_file.SetLength(requiredSize);

            _mmap_file = MemoryMappedFile.CreateFromFile(_data_file, null, 0,
                _read_only ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None, _read_only);
            _view_accessor = _mmap_file.CreateViewAccessor();
        }
        else if (_view_accessor is null)
        {
            _mmap_file = MemoryMappedFile.CreateFromFile(_data_file, null, 0,
                _read_only ? MemoryMappedFileAccess.Read : MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None, _read_only);
            _view_accessor = _mmap_file.CreateViewAccessor();
        }
    }

    #endregion

    #region 字段

    private readonly string _base_path;
    private readonly bool _read_only;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Stack<long> _free_pages;
    private long _max_page_id;
    private readonly FileStream _data_file;
    private MemoryMappedFile? _mmap_file;
    private MemoryMappedViewAccessor? _view_accessor;
    private bool _disposed;

    #endregion

    #region 属性

    /// <inheritdoc />
    public int page_size { get; }

    /// <inheritdoc />
    public long max_page_id => _max_page_id;

    #endregion

    #region 页面管理

    /// <inheritdoc />
    public long allocate_page()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_free_pages)
        {
            if (_free_pages.Count > 0) return _free_pages.Pop();
        }

        var newPageId = Interlocked.Increment(ref _max_page_id) - 1;
        ensure_mapped(newPageId + 1);
        return newPageId;
    }

    /// <inheritdoc />
    public void free_page(long pageId)
    {
        lock (_free_pages)
        {
            _free_pages.Push(pageId);
        }
    }

    #endregion

    #region 释放

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        await _lock.WaitAsync();
        try
        {
            _view_accessor?.Flush();
            _view_accessor?.Dispose();
            _mmap_file?.Dispose();
            await _data_file.DisposeAsync();
        }
        finally
        {
            _lock.Release();
            _lock.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    #endregion
}
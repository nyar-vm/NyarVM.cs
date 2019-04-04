using System.Collections.Concurrent;

namespace Std.Database.Storage;

/// <summary>
///     压缩存储引擎，在底层引擎之上透明压缩/解压页面数据
/// </summary>
internal sealed class CompressingStorageEngine : IStorageEngine
{
    #region 构造函数

    public CompressingStorageEngine(IStorageEngine inner, IPageCompressor compressor, string? metaPath = null)
    {
        _inner = inner;
        _compressor = compressor;
        page_size = inner.page_size;
        _compressed_sizes = new ConcurrentDictionary<long, int>();
        _meta_path = metaPath ?? "";

        if (!string.IsNullOrEmpty(_meta_path)) load_compressed_sizes();
    }

    #endregion

    #region 字段

    private readonly IStorageEngine _inner;
    private readonly IPageCompressor _compressor;
    private readonly string _meta_path;
    private bool _disposed;
    private bool _dirty;

    private readonly ConcurrentDictionary<long, int> _compressed_sizes;

    #endregion

    #region 属性

    public int page_size { get; }

    public long max_page_id => _inner.max_page_id;

    #endregion

    #region 读写

    public async ValueTask read(long pageId, Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (_compressed_sizes.TryGetValue(pageId, out var compressedSize))
        {
            var compressedBuffer =
                new byte[System.Math.Max(compressedSize, _compressor.max_compressed_size(page_size))];

            await _inner.read(pageId, compressedBuffer, cancellationToken);

            var decompressed = _compressor.decompress(compressedBuffer.AsSpan(0, compressedSize), buffer.Span);
            if (decompressed < 0) await _inner.read(pageId, buffer, cancellationToken);
        }
        else
        {
            await _inner.read(pageId, buffer, cancellationToken);
        }
    }

    public async ValueTask write(long pageId, ReadOnlyMemory<byte> data,
        CancellationToken cancellationToken = default)
    {
        var maxCompressedSize = _compressor.max_compressed_size(page_size);
        var compressedBuffer = new byte[maxCompressedSize];
        var compressedSize = _compressor.compress(data.Span, compressedBuffer);

        if (compressedSize > 0 && compressedSize < data.Length)
        {
            _compressed_sizes[pageId] = compressedSize;
            _dirty = true;
            await _inner.write(pageId, compressedBuffer.AsMemory(0, compressedSize), cancellationToken);
        }
        else
        {
            if (_compressed_sizes.TryRemove(pageId, out _)) _dirty = true;

            await _inner.write(pageId, data, cancellationToken);
        }
    }

    #endregion

    #region 页面管理

    public long allocate_page()
    {
        return _inner.allocate_page();
    }

    public void free_page(long pageId)
    {
        if (_compressed_sizes.TryRemove(pageId, out _)) _dirty = true;
        _inner.free_page(pageId);
    }

    #endregion

    #region 刷盘与释放

    public async ValueTask flush(CancellationToken cancellationToken = default)
    {
        await _inner.flush(cancellationToken);

        if (_dirty && !string.IsNullOrEmpty(_meta_path))
        {
            save_compressed_sizes();
            _dirty = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        if (_dirty && !string.IsNullOrEmpty(_meta_path))
        {
            save_compressed_sizes();
            _dirty = false;
        }

        _compressed_sizes.Clear();
        await _inner.DisposeAsync();
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    #endregion

    #region 持久化

    private void load_compressed_sizes()
    {
        if (string.IsNullOrEmpty(_meta_path) || !File.Exists(_meta_path)) return;

        try
        {
            using var fs = new FileStream(_meta_path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            var version = reader.ReadByte();
            if (version != 1) return;

            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                var pageId = reader.ReadInt64();
                var compressedSize = reader.ReadInt32();
                _compressed_sizes[pageId] = compressedSize;
            }
        }
        catch
        {
            _compressed_sizes.Clear();
        }
    }

    private void save_compressed_sizes()
    {
        if (string.IsNullOrEmpty(_meta_path)) return;

        try
        {
            var dir = Path.GetDirectoryName(_meta_path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            using var fs = new FileStream(_meta_path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs);

            writer.Write((byte)1);

            writer.Write(_compressed_sizes.Count);
            foreach (var (pageId, compressedSize) in _compressed_sizes)
            {
                writer.Write(pageId);
                writer.Write(compressedSize);
            }

            writer.Flush();
        }
        catch
        {
        }
    }

    #endregion
}
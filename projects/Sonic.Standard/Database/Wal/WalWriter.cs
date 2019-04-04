using Std.Database.Core;

namespace Std.Database.Wal;

/// <summary>
///     WAL 写入器实现，支持 EveryWrite / Batch / Periodic 三种刷盘策略，
///     以及批量序列化优化和 Brotli 压缩
/// </summary>
internal sealed class WalWriter : IWalWriter
{
    #region 属性

    /// <summary>
    ///     追加操作计数器（非数据库序列号，仅用于计数 WAL 写入次数）
    /// </summary>
    public SequenceNumber current_sequence => new((ulong)_append_count);

    #endregion

    #region 追加

    /// <inheritdoc />
    public async ValueTask append(WalRecord record, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _append_count++;

            var entry = build_disk_entry(record);

            switch (_flush_policy)
            {
                case WalFlushPolicy.every_write:
                    await _wal_file.WriteAsync(entry, cancellationToken);
                    await _wal_file.FlushAsync(cancellationToken);
                    break;

                case WalFlushPolicy.batch:
                    _batch_buffer.Add(entry);
                    if (_batch_buffer.Count >= _batch_size) await flush_batch(cancellationToken);

                    break;

                case WalFlushPolicy.periodic:
                    _batch_buffer.Add(entry);
                    if (_batch_buffer.Count >= _batch_size * 2) await flush_batch(cancellationToken);

                    break;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 截断

    /// <inheritdoc />
    public async ValueTask truncate(SequenceNumber sequence)
    {
        await _lock.WaitAsync();
        try
        {
            await flush_batch_core();

            var walPath = _wal_file.Name;
            await _wal_file.FlushAsync();
            await _wal_file.DisposeAsync();

            var tempPath = walPath + ".tmp";
            var keptEntries = new List<byte[]>();

            if (File.Exists(walPath))
                await using (var readStream = new FileStream(walPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    using var reader = new BinaryReader(readStream);
                    while (readStream.Position < readStream.Length)
                    {
                        var rawLength = reader.ReadInt32();
                        var isCompressed = (rawLength & _compression_flag) != 0;
                        var actualLength = rawLength & _length_mask;
                        var rawBuffer = reader.ReadBytes(actualLength);

                        byte[] decompressedBuffer;
                        if (isCompressed)
                            decompressedBuffer = WalRecordSerializer.decompress(rawBuffer);
                        else
                            decompressedBuffer = rawBuffer;

                        var record = WalRecordSerializer.deserialize(decompressedBuffer);

                        if (record.sequence.value > sequence.value)
                        {
                            var entry = new byte[_length_prefix_size + actualLength];
                            BitConverter.TryWriteBytes(entry.AsSpan(0, _length_prefix_size), rawLength);
                            rawBuffer.CopyTo(entry.AsSpan(_length_prefix_size));
                            keptEntries.Add(entry);
                        }
                    }
                }

            if (keptEntries.Count == 0)
            {
                if (File.Exists(walPath)) File.Delete(walPath);
            }
            else
            {
                await using (var writeStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write,
                                 FileShare.None))
                {
                    var totalSize = 0;
                    foreach (var entry in keptEntries) totalSize += entry.Length;

                    var combined = new byte[totalSize];
                    var offset = 0;
                    foreach (var entry in keptEntries)
                    {
                        entry.CopyTo(combined.AsSpan(offset));
                        offset += entry.Length;
                    }

                    await writeStream.WriteAsync(combined);
                    await writeStream.FlushAsync();
                }

                var backupPath = walPath + ".backup";
                if (File.Exists(backupPath)) File.Delete(backupPath);

                File.Move(walPath, backupPath);
                File.Move(tempPath, walPath);
                File.Delete(backupPath);
            }

            _wal_file = new FileStream(walPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096,
                FileOptions.SequentialScan);
        }
        finally
        {
            _lock.Release();
        }
    }

    #endregion

    #region 磁盘条目构建

    /// <summary>
    ///     构建单条记录的磁盘写入条目
    ///     格式：[4 字节长度（含压缩标志位）][数据]
    /// </summary>
    private byte[] build_disk_entry(WalRecord record)
    {
        var buffer = WalRecordSerializer.serialize(record);
        var isCompressed = _enable_compression && buffer.Length >= _compression_threshold;

        byte[] dataToWrite;
        if (isCompressed)
            dataToWrite = WalRecordSerializer.compress(buffer);
        else
            dataToWrite = buffer;

        var markedLength = isCompressed
            ? dataToWrite.Length | _compression_flag
            : dataToWrite.Length;

        var entry = new byte[_length_prefix_size + dataToWrite.Length];
        BitConverter.TryWriteBytes(entry.AsSpan(0, _length_prefix_size), markedLength);
        dataToWrite.CopyTo(entry.AsSpan(_length_prefix_size));

        return entry;
    }

    #endregion

    #region 常量

    private const int _compression_flag = unchecked((int)0x80000000);
    private const int _length_mask = 0x7FFFFFFF;
    private const int _length_prefix_size = sizeof(int);

    #endregion

    #region 字段

    private FileStream _wal_file;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private long _append_count;
    private readonly WalFlushPolicy _flush_policy;
    private readonly int _batch_size;
    private readonly int _periodic_flush_ms;
    private readonly List<byte[]> _batch_buffer;
    private readonly Timer? _periodic_timer;
    private bool _disposed;
    private readonly bool _enable_compression;
    private readonly int _compression_threshold;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建 WAL 写入器（使用默认刷盘策略）
    /// </summary>
    /// <param name="walPath">WAL 文件路径</param>
    public WalWriter(string walPath)
        : this(walPath, WalFlushPolicy.batch, 64, 100, false, 256)
    {
    }

    /// <summary>
    ///     创建 WAL 写入器（指定刷盘策略）
    /// </summary>
    /// <param name="walPath">WAL 文件路径</param>
    /// <param name="flushPolicy">刷盘策略</param>
    /// <param name="batchSize">批量写入的缓冲区大小（仅 Batch 模式）</param>
    /// <param name="periodicFlushMs">定时刷盘间隔毫秒（仅 Periodic 模式）</param>
    public WalWriter(string walPath, WalFlushPolicy flushPolicy, int batchSize = 64, int periodicFlushMs = 100)
        : this(walPath, flushPolicy, batchSize, periodicFlushMs, false, 256)
    {
    }

    /// <summary>
    ///     创建 WAL 写入器（指定刷盘策略和压缩选项）
    /// </summary>
    /// <param name="walPath">WAL 文件路径</param>
    /// <param name="flushPolicy">刷盘策略</param>
    /// <param name="batchSize">批量写入的缓冲区大小（仅 Batch 模式）</param>
    /// <param name="periodicFlushMs">定时刷盘间隔毫秒（仅 Periodic 模式）</param>
    /// <param name="enableCompression">是否启用 WAL 压缩</param>
    /// <param name="compressionThreshold">压缩阈值（字节），小于此大小的记录不压缩</param>
    public WalWriter(string walPath, WalFlushPolicy flushPolicy, int batchSize, int periodicFlushMs,
        bool enableCompression, int compressionThreshold)
    {
        var directory = Path.GetDirectoryName(walPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory)) Directory.CreateDirectory(directory);

        _wal_file = new FileStream(walPath, FileMode.Append, FileAccess.Write, FileShare.Read, 4096,
            FileOptions.SequentialScan);
        _flush_policy = flushPolicy;
        _batch_size = batchSize;
        _periodic_flush_ms = periodicFlushMs;
        _batch_buffer = new(batchSize);
        _enable_compression = enableCompression;
        _compression_threshold = compressionThreshold;

        if (_flush_policy == WalFlushPolicy.periodic)
            _periodic_timer = new Timer(
                async _ => await periodic_flush(),
                null,
                _periodic_flush_ms,
                _periodic_flush_ms);
    }

    #endregion

    #region 刷盘

    /// <inheritdoc />
    public async ValueTask flush(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            await flush_batch_core(cancellationToken);
            await _wal_file.FlushAsync(cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    ///     定时刷盘回调，异常不向上传播（Timer 回调不等待）
    /// </summary>
    private async ValueTask periodic_flush()
    {
        try
        {
            await _lock.WaitAsync();
            try
            {
                await flush_batch_core();
                await _wal_file.FlushAsync();
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (ObjectDisposedException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    /// <summary>
    ///     刷盘批量缓冲区（需在锁内调用）
    /// </summary>
    private async ValueTask flush_batch(CancellationToken cancellationToken = default)
    {
        await flush_batch_core(cancellationToken);
        await _wal_file.FlushAsync(cancellationToken);
    }

    /// <summary>
    ///     刷盘批量缓冲区核心逻辑（需在锁内调用）
    ///     将批量记录合并为连续字节序列，执行单次操作系统写入调用
    /// </summary>
    private async ValueTask flush_batch_core(CancellationToken cancellationToken = default)
    {
        if (_batch_buffer.Count == 0) return;

        var totalSize = 0;
        foreach (var entry in _batch_buffer) totalSize += entry.Length;

        var combined = new byte[totalSize];
        var offset = 0;
        foreach (var entry in _batch_buffer)
        {
            entry.CopyTo(combined.AsSpan(offset));
            offset += entry.Length;
        }

        await _wal_file.WriteAsync(combined, cancellationToken);
        _batch_buffer.Clear();
    }

    #endregion

    #region 释放

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;

        _periodic_timer?.Dispose();

        await _lock.WaitAsync();
        try
        {
            await flush_batch_core();
            await _wal_file.FlushAsync();
            await _wal_file.DisposeAsync();
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
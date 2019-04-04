using Olympus.Athena.Core;

namespace Olympus.Athena.Storage;

/// <summary>
///     微分区异步读取器，从文件系统读取持久化的微分区
/// </summary>
public sealed class MicroPartitionReader
{
    #region 字段

    private readonly string _basePath;

    #endregion

    #region 构造函数

    /// <summary>
    ///     创建微分区读取器
    /// </summary>
    /// <param name="basePath">数据存储目录路径</param>
    public MicroPartitionReader(string basePath)
    {
        _basePath = basePath;
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     从文件读取指定分区的微分区
    /// </summary>
    /// <param name="partitionId">分区标识</param>
    /// <param name="schema">列模式</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>微分区实例，文件不存在时返回 <c>null</c></returns>
    public async Task<MicroPartition?> ReadAsync(UUIDv7 partitionId, Schema schema, CancellationToken ct = default)
    {
        var fileName = $"{partitionId}.nyar";
        var filePath = Path.Combine(_basePath, fileName);

        if (!File.Exists(filePath)) return null;

        byte[] data;
        await using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
        {
            data = new byte[stream.Length];
            await stream.ReadExactlyAsync(data, ct);
        }

        return MicroPartition.Deserialize(data, schema);
    }

    /// <summary>
    ///     读取目录下所有微分区
    /// </summary>
    /// <param name="schema">列模式</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>微分区的异步可枚举序列</returns>
    public async IAsyncEnumerable<MicroPartition> ReadAllAsync(Schema schema,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (!Directory.Exists(_basePath)) yield break;

        var files = Directory.GetFiles(_basePath, "*.nyar");

        foreach (var file in files)
        {
            ct.ThrowIfCancellationRequested();

            MicroPartition? partition = null;

            try
            {
                byte[] data;
                await using (var stream =
                             new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                {
                    data = new byte[stream.Length];
                    await stream.ReadExactlyAsync(data, ct);
                }

                partition = MicroPartition.Deserialize(data, schema);
            }
            catch (AthenaException)
            {
                continue;
            }

            if (partition != null) yield return partition;
        }
    }

    /// <summary>
    ///     列出指定表的所有分区 ID
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分区 ID 的只读列表</returns>
    public Task<IReadOnlyList<UUIDv7>> ListPartitionsAsync(string tableName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var result = new List<UUIDv7>();

        if (!Directory.Exists(_basePath)) return Task.FromResult<IReadOnlyList<UUIDv7>>(result.AsReadOnly());

        var files = Directory.GetFiles(_basePath, "*.nyar");
        foreach (var file in files)
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
            if (Guid.TryParse(fileNameWithoutExt, out var guid)) result.Add(UUIDv7.FromGuid(guid));
        }

        return Task.FromResult<IReadOnlyList<UUIDv7>>(result.AsReadOnly());
    }

    /// <summary>
    ///     从文件读取指定分区的元数据（仅读取头部，不加载列数据）
    /// </summary>
    /// <param name="partitionId">分区标识</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>分区元数据实例，文件不存在或格式错误时返回 <c>null</c></returns>
    public async Task<PartitionMetadata?> ReadMetadataAsync(UUIDv7 partitionId, CancellationToken ct = default)
    {
        var fileName = $"{partitionId}.nyar";
        var filePath = Path.Combine(_basePath, fileName);

        if (!File.Exists(filePath)) return null;

        try
        {
            await using var stream =
                new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

            var headerBuf = new byte[1024];
            var bytesRead = await stream.ReadAsync(headerBuf, ct);

            if (bytesRead < 24) return null;

            var span = headerBuf.AsSpan();
            var offset = 0;

            var high = BitConverter.ToUInt64(span[offset..]);
            offset += sizeof(ulong);
            var low = BitConverter.ToUInt64(span[offset..]);
            offset += sizeof(ulong);
            var filePartitionId = new UUIDv7(high, low);

            if (!filePartitionId.Equals(partitionId)) return null;

            var rowCount = BitConverter.ToInt32(span[offset..]);
            offset += sizeof(int);
            var columnMapCount = BitConverter.ToInt32(span[offset..]);
            offset += sizeof(int);

            var columnMaps = new Dictionary<int, ColumnZoneMap>();
            for (var i = 0; i < columnMapCount; i++)
            {
                if (offset + 40 > bytesRead) break;

                var colIndex = BitConverter.ToInt32(span[offset..]);
                offset += sizeof(int);
                var min = BitConverter.ToInt64(span[offset..]);
                offset += sizeof(long);
                var max = BitConverter.ToInt64(span[offset..]);
                offset += sizeof(long);
                var nullCount = BitConverter.ToInt32(span[offset..]);
                offset += sizeof(int);
                var colOffset = BitConverter.ToInt64(span[offset..]);
                offset += sizeof(long);
                var length = BitConverter.ToInt32(span[offset..]);
                offset += sizeof(int);
                var encoding = (EncodingType)BitConverter.ToInt32(span[offset..]);
                offset += sizeof(int);

                columnMaps[colIndex] = new ColumnZoneMap(min, max, nullCount, colOffset, length, encoding);
            }

            return PartitionMetadata.Create(filePartitionId, rowCount, columnMaps);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     从文件读取指定分区的完整微分区
    /// </summary>
    /// <param name="partitionId">分区标识</param>
    /// <param name="schema">列模式</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>微分区实例，文件不存在时返回 <c>null</c></returns>
    public Task<MicroPartition?> ReadPartitionAsync(UUIDv7 partitionId, Schema schema, CancellationToken ct = default)
    {
        return ReadAsync(partitionId, schema, ct);
    }

    #endregion
}
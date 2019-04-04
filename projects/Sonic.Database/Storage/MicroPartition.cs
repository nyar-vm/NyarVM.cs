using Olympus.Athena.Core;

namespace Olympus.Athena.Storage;

/// <summary>
///     列式微分区，以 PAX 布局存储一组列数据，支持行级追加和批量列操作
/// </summary>
public sealed class MicroPartition : IDisposable
{
    #region 常量

    private const int PageSize = 4096;

    #endregion

    #region 构造函数

    /// <summary>
    ///     使用分区标识和列模式创建微分区
    /// </summary>
    /// <param name="partitionId">分区唯一标识</param>
    /// <param name="schema">列模式</param>
    public MicroPartition(UUIDv7 partitionId, Schema schema)
    {
        PartitionId = partitionId;
        _schema = schema;
        _columns = new Dictionary<int, object>();
        _pages = [];
        Metadata = PartitionMetadata.Create(partitionId, 0, new Dictionary<int, ColumnZoneMap>());
    }

    #endregion

    #region 字段

    private readonly Schema _schema;
    private readonly Dictionary<int, object> _columns;
    private List<byte[]> _pages;
    private bool _disposed;

    #endregion

    #region 属性

    /// <summary>
    ///     分区唯一标识
    /// </summary>
    public UUIDv7 PartitionId { get; }

    /// <summary>
    ///     分区元数据
    /// </summary>
    public PartitionMetadata Metadata { get; private set; }

    /// <summary>
    ///     行数，取所有列中最小的值个数
    /// </summary>
    public int RowCount
    {
        get
        {
            var min = int.MaxValue;
            for (var i = 0; i < _schema.Count; i++)
                if (_columns.TryGetValue(i, out var chunk))
                {
                    var count = GetChunkCount(chunk, _schema[i].DataType);
                    if (count < min) min = count;
                }
                else
                {
                    return 0;
                }

            return min == int.MaxValue ? 0 : min;
        }
    }

    /// <summary>
    ///     需要的 Page 数量（按 4KB 计算）
    /// </summary>
    public int PageCount
    {
        get
        {
            var serialized = Serialize();
            return (serialized.Length + PageSize - 1) / PageSize;
        }
    }

    #endregion

    #region 公共方法

    /// <summary>
    ///     追加一行中某列的值
    /// </summary>
    /// <param name="columnIndex">列索引</param>
    /// <param name="value">值</param>
    public void AppendRow(int columnIndex, DataValue value)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MicroPartition));

        if (columnIndex < 0 || columnIndex >= _schema.Count)
            throw new AthenaException($"列索引 {columnIndex} 超出范围（0-{_schema.Count - 1}）");

        var colDesc = _schema[columnIndex];
        AppendRowElement(columnIndex, colDesc.DataType, value);
    }

    /// <summary>
    ///     批量写入列数据
    /// </summary>
    /// <typeparam name="T">非托管值类型</typeparam>
    /// <param name="columnIndex">列索引</param>
    /// <param name="values">值集合</param>
    public void WriteColumn<T>(int columnIndex, ReadOnlySpan<T> values) where T : unmanaged
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MicroPartition));

        if (columnIndex < 0 || columnIndex >= _schema.Count) throw new AthenaException($"列索引 {columnIndex} 超出范围");

        if (_columns.TryGetValue(columnIndex, out var existing))
        {
            var chunk = (ColumnChunk<T>)existing;
            chunk.Append(values);
        }
        else
        {
            _columns[columnIndex] = new ColumnChunk<T>(values);
        }
    }

    /// <summary>
    ///     批量读取列数据
    /// </summary>
    /// <typeparam name="T">非托管值类型</typeparam>
    /// <param name="columnIndex">列索引</param>
    /// <returns>列数据的只读列表</returns>
    public IReadOnlyList<T> ReadColumn<T>(int columnIndex) where T : unmanaged
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MicroPartition));

        if (!_columns.TryGetValue(columnIndex, out var obj)) return [];

        var chunk = (ColumnChunk<T>)obj;
        return chunk.GetValues();
    }

    /// <summary>
    ///     序列化为字节数组（PAX 布局）
    /// </summary>
    /// <returns>序列化后的字节数组</returns>
    public byte[] Serialize()
    {
        var headerData = BuildHeader();
        var columnData = BuildColumnData();
        var totalSize = headerData.Length + columnData.Length;
        var result = new byte[totalSize];
        Array.Copy(headerData, 0, result, 0, headerData.Length);
        Array.Copy(columnData, 0, result, headerData.Length, columnData.Length);
        return result;
    }

    /// <summary>
    ///     获取分页后的数据用于存储
    /// </summary>
    /// <returns>分页数据列表，每页 4KB</returns>
    public IReadOnlyList<byte[]> GetPages()
    {
        var serialized = Serialize();
        _pages.Clear();

        for (var offset = 0; offset < serialized.Length; offset += PageSize)
        {
            var remaining = serialized.Length - offset;
            var pageLength = Math.Min(PageSize, remaining);
            var page = new byte[pageLength];
            Array.Copy(serialized, offset, page, 0, pageLength);
            _pages.Add(page);
        }

        return _pages.AsReadOnly();
    }

    /// <summary>
    ///     从 Page 列表组装微分区
    /// </summary>
    /// <param name="pages">Page 数据列表</param>
    /// <param name="schema">列模式</param>
    /// <returns>微分区实例</returns>
    public static MicroPartition FromPages(IReadOnlyList<byte[]> pages, Schema schema)
    {
        var totalSize = 0;
        for (var i = 0; i < pages.Count; i++) totalSize += pages[i].Length;

        var data = new byte[totalSize];
        var offset = 0;
        for (var i = 0; i < pages.Count; i++)
        {
            Array.Copy(pages[i], 0, data, offset, pages[i].Length);
            offset += pages[i].Length;
        }

        return Deserialize(data, schema);
    }

    /// <summary>
    ///     从字节数组反序列化微分区
    /// </summary>
    /// <param name="data">序列化数据</param>
    /// <param name="schema">列模式</param>
    /// <returns>微分区实例</returns>
    public static MicroPartition Deserialize(byte[] data, Schema schema)
    {
        var span = data.AsSpan();
        var offset = 0;

        var partitionId = ReadPartitionId(span, ref offset);
        var rowCount = BitConverter.ToInt32(span[offset..]);
        offset += sizeof(int);
        var columnMapCount = BitConverter.ToInt32(span[offset..]);
        offset += sizeof(int);

        var columnMaps = new Dictionary<int, ColumnZoneMap>();
        for (var i = 0; i < columnMapCount; i++)
        {
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

        var headerSize = offset;
        var partition = new MicroPartition(partitionId, schema);

        for (var i = 0; i < schema.Count; i++)
        {
            if (!columnMaps.TryGetValue(i, out var map)) continue;

            var colData = data.AsSpan((int)(headerSize + map.Offset), map.Length);
            var colDesc = schema[i];
            var chunk = CreateColumnChunk(colData, map.Encoding, rowCount, colDesc.DataType);
            if (chunk != null) partition._columns[i] = chunk;
        }

        partition.Metadata = PartitionMetadata.Create(partitionId, rowCount, columnMaps);
        partition._pages = [];

        return partition;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _columns.Clear();
        _pages.Clear();
    }

    #endregion

    #region 内部辅助 — 行追加

    private void AppendRowElement(int columnIndex, DataType dataType, DataValue value)
    {
        switch (dataType)
        {
            case DataType.Int64:
            {
                var v = value.TryGetInt64(out var i64) ? i64 : 0L;
                AppendTyped(columnIndex, v);
                break;
            }
            case DataType.Float64:
            {
                var v = value.TryGetFloat64(out var f64) ? f64 : 0.0;
                AppendTyped(columnIndex, v);
                break;
            }
            case DataType.Bool:
            {
                var v = value.TryGetBool(out var b) && b;
                AppendTyped(columnIndex, v);
                break;
            }
            case DataType.Guid:
            {
                var v = value.TryGetGuid(out var g) ? g : Guid.Empty;
                AppendTyped(columnIndex, v);
                break;
            }
            default:
                throw new AthenaException($"列 {columnIndex} 的数据类型 {dataType} 不支持列式存储");
        }
    }

    private void AppendTyped<T>(int columnIndex, T value) where T : unmanaged
    {
        if (_columns.TryGetValue(columnIndex, out var existing))
        {
            var chunk = (ColumnChunk<T>)existing;
            chunk.Append(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
        }
        else
        {
            _columns[columnIndex] = new ColumnChunk<T>(MemoryMarshal.CreateReadOnlySpan(ref value, 1));
        }
    }

    #endregion

    #region 内部辅助 — 序列化

    private byte[] BuildHeader()
    {
        var columnMaps = BuildColumnMaps();
        var mapCount = columnMaps.Count;
        var mapEntrySize = sizeof(int) + sizeof(long) * 2 + sizeof(int) + sizeof(long) + sizeof(int) + sizeof(int);
        var headerSize = sizeof(ulong) * 2 + sizeof(int) * 2 + mapEntrySize * mapCount;
        var header = new byte[headerSize];
        var offset = 0;

        BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(ulong)), PartitionId.High);
        offset += sizeof(ulong);
        BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(ulong)), PartitionId.Low);
        offset += sizeof(ulong);

        var rowCount = RowCount;
        BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(int)), rowCount);
        offset += sizeof(int);
        BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(int)), mapCount);
        offset += sizeof(int);

        foreach (var (colIndex, map) in columnMaps)
        {
            BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(int)), colIndex);
            offset += sizeof(int);
            BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(long)), map.Min);
            offset += sizeof(long);
            BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(long)), map.Max);
            offset += sizeof(long);
            BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(int)), map.NullCount);
            offset += sizeof(int);
            BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(long)), map.Offset);
            offset += sizeof(long);
            BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(int)), map.Length);
            offset += sizeof(int);
            BitConverter.TryWriteBytes(header.AsSpan(offset, sizeof(int)), (int)map.Encoding);
            offset += sizeof(int);
        }

        return header;
    }

    private Dictionary<int, ColumnZoneMap> BuildColumnMaps()
    {
        var maps = new Dictionary<int, ColumnZoneMap>();
        var currentOffset = 0L;

        for (var i = 0; i < _schema.Count; i++)
        {
            if (!_columns.TryGetValue(i, out var obj)) continue;

            var (data, encoding, count) = GetChunkInfo(obj, _schema[i].DataType);
            var (min, max) = ComputeMinMax(obj, _schema[i].DataType, count);
            var nullCount = 0;

            maps[i] = new ColumnZoneMap(min, max, nullCount, currentOffset, data.Length, encoding);
            currentOffset += data.Length;
        }

        return maps;
    }

    private byte[] BuildColumnData()
    {
        using var stream = new MemoryStream();
        for (var i = 0; i < _schema.Count; i++)
        {
            if (!_columns.TryGetValue(i, out var obj)) continue;

            var (data, _, _) = GetChunkInfo(obj, _schema[i].DataType);
            stream.Write(data);
        }

        return stream.ToArray();
    }

    #endregion

    #region 内部辅助 — 列信息获取

    private static int GetChunkCount(object chunk, DataType dataType)
    {
        return dataType switch
        {
            DataType.Int64 => ((ColumnChunk<long>)chunk).Count,
            DataType.Float64 => ((ColumnChunk<double>)chunk).Count,
            DataType.Bool => ((ColumnChunk<bool>)chunk).Count,
            DataType.Guid => ((ColumnChunk<Guid>)chunk).Count,
            _ => 0
        };
    }

    private static (byte[] Data, EncodingType Encoding, int Count) GetChunkInfo(object chunk, DataType dataType)
    {
        return dataType switch
        {
            DataType.Int64 => GetInfo<long>(chunk),
            DataType.Float64 => GetInfo<double>(chunk),
            DataType.Bool => GetInfo<bool>(chunk),
            DataType.Guid => GetInfo<Guid>(chunk),
            _ => throw new AthenaException($"数据类型 {dataType} 不支持列信息获取")
        };
    }

    private static (byte[] Data, EncodingType Encoding, int Count) GetInfo<T>(object chunk) where T : unmanaged
    {
        var c = (ColumnChunk<T>)chunk;
        return (c.RawData.ToArray(), c.Encoding, c.Count);
    }

    private static (long Min, long Max) ComputeMinMax(object chunk, DataType dataType, int count)
    {
        if (count == 0) return (0, 0);

        return dataType switch
        {
            DataType.Int64 => ComputeMinMaxTyped((ColumnChunk<long>)chunk),
            DataType.Float64 => ComputeMinMaxTypedDouble((ColumnChunk<double>)chunk),
            DataType.Bool => ComputeMinMaxTyped((ColumnChunk<bool>)chunk),
            DataType.Guid => ComputeMinMaxTypedGuid((ColumnChunk<Guid>)chunk),
            _ => (0, 0)
        };
    }

    private static (long Min, long Max) ComputeMinMaxTyped<T>(ColumnChunk<T> chunk) where T : unmanaged
    {
        var values = chunk.GetValues();
        if (values.Length == 0) return (0, 0);

        var first = ToInt64Generic(values[0]);
        var min = first;
        var max = first;

        for (var i = 1; i < values.Length; i++)
        {
            var val = ToInt64Generic(values[i]);
            if (val < min) min = val;

            if (val > max) max = val;
        }

        return (min, max);
    }

    private static (long Min, long Max) ComputeMinMaxTypedDouble(ColumnChunk<double> chunk)
    {
        var values = chunk.GetValues();
        if (values.Length == 0) return (0, 0);

        var first = BitConverter.DoubleToInt64Bits(values[0]);
        var min = first;
        var max = first;

        for (var i = 1; i < values.Length; i++)
        {
            var val = BitConverter.DoubleToInt64Bits(values[i]);
            if (val < min) min = val;

            if (val > max) max = val;
        }

        return (min, max);
    }

    private static (long Min, long Max) ComputeMinMaxTypedGuid(ColumnChunk<Guid> chunk)
    {
        var values = chunk.GetValues();
        if (values.Length == 0) return (0, 0);

        var bytes = values[0].ToByteArray();
        var first = BitConverter.ToInt64(bytes, 0);
        var min = first;
        var max = first;

        for (var i = 1; i < values.Length; i++)
        {
            bytes = values[i].ToByteArray();
            var val = BitConverter.ToInt64(bytes, 0);
            if (val < min) min = val;

            if (val > max) max = val;
        }

        return (min, max);
    }

    private static long ToInt64Generic<T>(T value) where T : unmanaged
    {
        if (typeof(T) == typeof(long)) return (long)(object)value;

        if (typeof(T) == typeof(int)) return (int)(object)value;

        if (typeof(T) == typeof(short)) return (short)(object)value;

        if (typeof(T) == typeof(byte)) return (byte)(object)value;

        if (typeof(T) == typeof(bool)) return (bool)(object)value ? 1 : 0;

        return 0;
    }

    #endregion

    #region 内部辅助 — 反序列化

    private static UUIDv7 ReadPartitionId(ReadOnlySpan<byte> span, ref int offset)
    {
        var high = BitConverter.ToUInt64(span[offset..]);
        offset += sizeof(ulong);
        var low = BitConverter.ToUInt64(span[offset..]);
        offset += sizeof(ulong);
        return new UUIDv7(high, low);
    }

    private static object? CreateColumnChunk(ReadOnlySpan<byte> data, EncodingType encoding, int count,
        DataType dataType)
    {
        return dataType switch
        {
            DataType.Int64 => ColumnChunk<long>.FromRaw(data, encoding, count),
            DataType.Float64 => ColumnChunk<double>.FromRaw(data, encoding, count),
            DataType.Bool => ColumnChunk<bool>.FromRaw(data, encoding, count),
            DataType.Guid => ColumnChunk<Guid>.FromRaw(data, encoding, count),
            _ => throw new AthenaException($"数据类型 {dataType} 不支持列式反序列化")
        };
    }

    #endregion
}
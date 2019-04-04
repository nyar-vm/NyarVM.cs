using Olympus.Athena.Core;

namespace Olympus.Athena.Storage;

#region ColumnZoneMap 列区域映射

/// <summary>
///     列的区域映射统计信息，记录微分区内某列的最小值、最大值、空值个数和存储位置
/// </summary>
public readonly struct ColumnZoneMap
{
    /// <summary>
    ///     列最小值
    /// </summary>
    public long Min { get; }

    /// <summary>
    ///     列最大值
    /// </summary>
    public long Max { get; }

    /// <summary>
    ///     空值个数
    /// </summary>
    public int NullCount { get; }

    /// <summary>
    ///     列数据在分区中的字节偏移
    /// </summary>
    public long Offset { get; }

    /// <summary>
    ///     列数据长度（字节数）
    /// </summary>
    public int Length { get; }

    /// <summary>
    ///     列数据的编码类型
    /// </summary>
    public EncodingType Encoding { get; }

    /// <summary>
    ///     创建列区域映射
    /// </summary>
    /// <param name="min">列最小值</param>
    /// <param name="max">列最大值</param>
    /// <param name="nullCount">空值个数</param>
    /// <param name="offset">列数据字节偏移</param>
    /// <param name="length">列数据字节长度</param>
    /// <param name="encoding">编码类型</param>
    public ColumnZoneMap(long min, long max, int nullCount, long offset, int length, EncodingType encoding)
    {
        Min = min;
        Max = max;
        NullCount = nullCount;
        Offset = offset;
        Length = length;
        Encoding = encoding;
    }
}

#endregion

#region PartitionMetadata 分区元数据

/// <summary>
///     微分区的元数据描述，包含分区标识、行数和各列的区域映射
/// </summary>
public sealed class PartitionMetadata
{
    private PartitionMetadata(UUIDv7 partitionId, int rowCount, Dictionary<int, ColumnZoneMap> columnMaps)
    {
        PartitionId = partitionId;
        RowCount = rowCount;
        ColumnMaps = columnMaps;
    }

    /// <summary>
    ///     分区唯一标识
    /// </summary>
    public UUIDv7 PartitionId { get; }

    /// <summary>
    ///     行数
    /// </summary>
    public int RowCount { get; }

    /// <summary>
    ///     各列的区域映射，Key 为列索引
    /// </summary>
    public Dictionary<int, ColumnZoneMap> ColumnMaps { get; }

    /// <summary>
    ///     创建分区元数据
    /// </summary>
    /// <param name="partitionId">分区标识</param>
    /// <param name="rowCount">行数</param>
    /// <param name="maps">各列的区域映射字典</param>
    /// <returns>分区元数据实例</returns>
    public static PartitionMetadata Create(UUIDv7 partitionId, int rowCount, Dictionary<int, ColumnZoneMap> maps)
    {
        return new PartitionMetadata(partitionId, rowCount, maps);
    }
}

#endregion
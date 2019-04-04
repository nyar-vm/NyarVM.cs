using Olympus.Athena.Core;
using Olympus.Athena.Storage;

namespace Olympus.Athena.Index;

#region ZoneMapIndex Zone Map 分区剪枝索引

/// <summary>
///     基于 Zone Map 的分区剪枝索引，利用各列的 min/max 统计信息快速过滤无关分区
/// </summary>
public sealed class ZoneMapIndex
{
    #region 字段

    private readonly IReadOnlyList<PartitionMetadata> _partitions;

    #endregion

    #region 构造函数

    /// <summary>
    ///     使用分区元数据集合创建 Zone Map 索引
    /// </summary>
    /// <param name="partitions">微分区元数据集合</param>
    public ZoneMapIndex(IReadOnlyList<PartitionMetadata> partitions)
    {
        _partitions = partitions;
    }

    #endregion

    #region 范围过滤

    /// <summary>
    ///     返回指定列的值在 [<paramref name="min" />, <paramref name="max" />] 范围内的分区 ID 列表
    /// </summary>
    /// <param name="columnIndex">列索引</param>
    /// <param name="min">最小值（包含）</param>
    /// <param name="max">最大值（包含）</param>
    /// <returns>满足条件的分区 ID 的只读列表</returns>
    public IReadOnlyList<UUIDv7> Filter(int columnIndex, long min, long max)
    {
        var result = new List<UUIDv7>();
        foreach (var partition in _partitions)
        {
            if (!partition.ColumnMaps.TryGetValue(columnIndex, out var zoneMap)) continue;

            if (zoneMap.Min <= max && zoneMap.Max >= min) result.Add(partition.PartitionId);
        }

        return result;
    }

    #endregion

    #region 非空过滤

    /// <summary>
    ///     返回指定列非全为 null 的分区 ID 列表
    /// </summary>
    /// <param name="columnIndex">列索引</param>
    /// <returns>该列至少存在一个非 null 值的分区 ID 的只读列表</returns>
    public IReadOnlyList<UUIDv7> FilterNotNull(int columnIndex)
    {
        var result = new List<UUIDv7>();
        foreach (var partition in _partitions)
        {
            if (!partition.ColumnMaps.TryGetValue(columnIndex, out var zoneMap)) continue;

            if (zoneMap.NullCount < partition.RowCount) result.Add(partition.PartitionId);
        }

        return result;
    }

    #endregion
}

#endregion
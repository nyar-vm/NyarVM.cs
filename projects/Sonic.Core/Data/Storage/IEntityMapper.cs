using System.Collections.Generic;

namespace Core.Data.Storage;

/// <summary>
///     实体映射器接口，定义实体与存储表之间的映射契约。
/// </summary>
/// <typeparam name="T">实体类型。</typeparam>
public interface IEntityMapper<T>
{
    /// <summary>
    ///     获取目标存储表名称。
    /// </summary>
    string table_name { get; }

    /// <summary>
    ///     获取所有列的元数据列表。
    /// </summary>
    IReadOnlyList<ColumnMeta> columns { get; }

    /// <summary>
    ///     获取主键列的索引列表。
    /// </summary>
    IReadOnlyList<int> primary_keys { get; }

    /// <summary>
    ///     获取并发检查字段的列索引，无并发字段时为 null。
    /// </summary>
    int? concurrency_field { get; }
}
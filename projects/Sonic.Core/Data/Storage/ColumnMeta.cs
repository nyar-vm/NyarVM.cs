namespace Core.Data.Storage;

/// <summary>
///     列元数据，描述存储列的名称、类型和约束信息。
/// </summary>
public sealed class ColumnMeta
{
    /// <summary>
    ///     初始化列元数据。
    /// </summary>
    /// <param name="memberName">成员名称。</param>
    /// <param name="columnName">列名称。</param>
    /// <param name="storageType">存储类型。</param>
    /// <param name="isNullable">是否允许为空。</param>
    /// <param name="isUnique">是否具有唯一约束。</param>
    /// <param name="isIndexed">是否已建立索引。</param>
    /// <param name="defaultValue">默认值。</param>
    public ColumnMeta(
        string memberName,
        string columnName,
        StorageType storageType,
        bool isNullable,
        bool isUnique,
        bool isIndexed,
        object? defaultValue = null)
    {
        member_name = memberName;
        column_name = columnName;
        storage_type = storageType;
        is_nullable = isNullable;
        is_unique = isUnique;
        is_indexed = isIndexed;
        default_value = defaultValue;
    }

    /// <summary>
    ///     成员名称。
    /// </summary>
    public string member_name { get; }

    /// <summary>
    ///     列名称。
    /// </summary>
    public string column_name { get; }

    /// <summary>
    ///     存储类型。
    /// </summary>
    public StorageType storage_type { get; }

    /// <summary>
    ///     是否允许为空。
    /// </summary>
    public bool is_nullable { get; }

    /// <summary>
    ///     是否具有唯一约束。
    /// </summary>
    public bool is_unique { get; }

    /// <summary>
    ///     是否已建立索引。
    /// </summary>
    public bool is_indexed { get; }

    /// <summary>
    ///     默认值。
    /// </summary>
    public object? default_value { get; }
}
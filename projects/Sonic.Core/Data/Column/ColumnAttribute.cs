using System;

namespace Core.Data.Column;

/// <summary>
///     标记属性为存储列，指定列名和存储类型。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class ColumnAttribute : Attribute
{
    /// <summary>
    ///     列名称。
    /// </summary>
    public string? name { get; set; }

    /// <summary>
    ///     列的存储类型，对应 <see cref="Storage.StorageType" /> 枚举值。
    /// </summary>
    public int storage_type { get; set; }
}
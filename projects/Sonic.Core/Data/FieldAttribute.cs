using System;

namespace Core.Data;

/// <summary>
///     标记字段的元数据，包括名称、顺序、默认值、必填性和空值跳过行为。
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class FieldAttribute : Attribute
{
    /// <summary>
    ///     字段的自定义名称，用于序列化和反序列化时的映射。
    /// </summary>
    public string? name { get; set; }

    /// <summary>
    ///     字段的排序顺序，-1 表示未指定。
    /// </summary>
    public int order { get; set; } = -1;

    /// <summary>
    ///     字段的默认值。
    /// </summary>
    public object? default_value { get; set; }

    /// <summary>
    ///     字段是否为必填项。
    /// </summary>
    public bool required { get; set; } = false;

    /// <summary>
    ///     当字段值为 null 时是否跳过序列化。
    /// </summary>
    public bool skip_when_null { get; set; } = false;
}

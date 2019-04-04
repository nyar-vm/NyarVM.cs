namespace Std.Binary.Attributes;

/// <summary>
///     标记偏移表字段，指定偏移来源和目标类型�?///
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class OffsetTableAttribute : Attribute
{
    /// <summary>
    ///     引用存储偏移的字段名�?    ///
    /// </summary>
    public string offset_field { get; set; } = "";

    /// <summary>
    ///     目标结构体类型�?    ///
    /// </summary>
    public Type target_type { get; set; } = typeof(object);

    /// <summary>
    ///     偏移基准：start / header_end 等�?    ///
    /// </summary>
    public string relative_to { get; set; } = "start";
}
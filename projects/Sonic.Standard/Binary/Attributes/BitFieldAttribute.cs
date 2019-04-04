namespace Std.Binary.Attributes;

/// <summary>
///     标记位域字段，指定在宿主字段中的位偏移和位数�?///
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class BitFieldAttribute : Attribute
{
    /// <summary>
    ///     起始位（0 = 最低位）�?    ///
    /// </summary>
    public int bit_offset { get; set; }

    /// <summary>
    ///     占用位数�? �?64）�?    ///
    /// </summary>
    public int bit_count { get; set; }
}
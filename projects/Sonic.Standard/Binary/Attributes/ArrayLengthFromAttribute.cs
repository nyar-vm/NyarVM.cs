namespace Std.Binary.Attributes;

/// <summary>
///     标记数组字段，指定其长度来自另一个字段�?///
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ArrayLengthFromAttribute : Attribute
{
    /// <summary>
    ///     存储数组长度的字段名�?    ///
    /// </summary>
    public string field_name { get; set; } = "";
}
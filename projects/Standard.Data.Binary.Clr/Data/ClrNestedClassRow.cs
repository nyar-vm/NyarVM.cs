namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     NestedClass 表行（ECMA-335 §22.32）的
/// </summary>
public sealed class ClrNestedClassRow
{
    public uint nested_class_index { get; init; }
    public uint enclosing_class_index { get; init; }
}
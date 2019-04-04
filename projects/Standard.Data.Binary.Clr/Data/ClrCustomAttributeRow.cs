namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     CustomAttribute 表行（ECMA-335 §22.10）的
/// </summary>
public sealed class ClrCustomAttributeRow
{
    public uint parent_index { get; init; }
    public uint type_index { get; init; }
    public uint value_index { get; init; }
}
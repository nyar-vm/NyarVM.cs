namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     File 表行（ECMA-335 §22.16）的
/// </summary>
public sealed class ClrFileRow
{
    public uint flags { get; init; }
    public string name { get; init; } = string.Empty;
    public uint hash_value_index { get; init; }
}
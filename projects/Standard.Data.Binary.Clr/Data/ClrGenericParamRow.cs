namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     GenericParam 表行（ECMA-335 §22.20）的
/// </summary>
public sealed class ClrGenericParamRow
{
    public ushort number { get; init; }
    public ushort flags { get; init; }
    public uint owner_index { get; init; }
    public string name { get; init; } = string.Empty;
}
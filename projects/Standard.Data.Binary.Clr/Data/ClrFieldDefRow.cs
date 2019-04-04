namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     Field 表行（ECMA-335 §22.15）的
/// </summary>
public sealed class ClrFieldDefRow
{
    public ClrFieldAttributes flags { get; init; }
    public string name { get; init; } = string.Empty;
    public uint signature_index { get; init; }
}
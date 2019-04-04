namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     Property 表行（ECMA-335 §22.34）的
/// </summary>
public sealed class ClrPropertyDefRow
{
    public ushort flags { get; init; }
    public string name { get; init; } = string.Empty;
    public uint signature_index { get; init; }
}
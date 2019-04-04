namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     Event 表行（ECMA-335 §22.13）的
/// </summary>
public sealed class ClrEventDefRow
{
    public ushort event_flags { get; init; }
    public string name { get; init; } = string.Empty;
    public uint event_type_index { get; init; }
}
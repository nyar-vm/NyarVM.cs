namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     AssemblyRef 表行（ECMA-335 §22.5）的
/// </summary>
public sealed class ClrAssemblyRefRow
{
    public ushort major_version { get; init; }
    public ushort minor_version { get; init; }
    public ushort build_number { get; init; }
    public ushort revision_number { get; init; }
    public uint flags { get; init; }
    public uint public_key_or_token_index { get; init; }
    public string name { get; init; } = string.Empty;
    public string culture { get; init; } = string.Empty;
    public uint hash_value_index { get; init; }
}
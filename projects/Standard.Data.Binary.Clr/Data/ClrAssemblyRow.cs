namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     Assembly 表行（ECMA-335 §22.2）的
/// </summary>
public sealed class ClrAssemblyRow
{
    public uint hash_alg_id { get; init; }
    public ushort major_version { get; init; }
    public ushort minor_version { get; init; }
    public ushort build_number { get; init; }
    public ushort revision_number { get; init; }
    public uint flags { get; init; }
    public uint public_key_index { get; init; }
    public string name { get; init; } = string.Empty;
    public string culture { get; init; } = string.Empty;
}
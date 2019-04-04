namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     MethodDef 表行（ECMA-335 §22.26）的
/// </summary>
public sealed class ClrMethodDefRow
{
    public uint rva { get; init; }
    public ushort impl_flags { get; init; }
    public ClrMethodAttributes flags { get; init; }
    public string name { get; init; } = string.Empty;
    public uint signature_index { get; init; }
    public int param_list_start { get; init; }
}
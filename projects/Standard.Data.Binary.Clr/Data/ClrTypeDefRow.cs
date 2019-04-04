namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     TypeDef 表行（ECMA-335 §22.37）的
/// </summary>
public sealed class ClrTypeDefRow
{
    public ClrTypeAttributes flags { get; init; }
    public string name { get; init; } = string.Empty;
    public string @namespace { get; init; } = string.Empty;
    public uint extends_index { get; init; }
    public int field_list_start { get; init; }
    public int method_list_start { get; init; }
}
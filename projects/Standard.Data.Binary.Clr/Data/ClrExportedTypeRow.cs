namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     ExportedType 表行（ECMA-335 §22.14）的
/// </summary>
public sealed class ClrExportedTypeRow
{
    public uint flags { get; init; }
    public uint type_def_id { get; init; }
    public string name { get; init; } = string.Empty;
    public string @namespace { get; init; } = string.Empty;
    public uint implementation_index { get; init; }
}
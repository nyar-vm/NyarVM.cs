namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     TypeRef 表行（ECMA-335 §22.38）的
/// </summary>
public sealed class ClrTypeRefRow
{
    public uint resolution_scope_index { get; init; }
    public string name { get; init; } = string.Empty;
    public string @namespace { get; init; } = string.Empty;
}
namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     ManifestResource 表行（ECMA-335 §22.24）的
/// </summary>
public sealed class ClrManifestResourceRow
{
    public uint offset { get; init; }
    public uint flags { get; init; }
    public uint implementation_index { get; init; }
    public string name { get; init; } = string.Empty;
}
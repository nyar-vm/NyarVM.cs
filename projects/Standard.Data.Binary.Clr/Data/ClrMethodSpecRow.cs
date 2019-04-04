namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     MethodSpec 表行（ECMA-335 §22.27）的
/// </summary>
public sealed class ClrMethodSpecRow
{
    public uint method_index { get; init; }
    public uint instantiation_index { get; init; }
}
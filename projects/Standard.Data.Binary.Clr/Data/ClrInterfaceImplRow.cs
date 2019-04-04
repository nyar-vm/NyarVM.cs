namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     InterfaceImpl 表行（ECMA-335 §22.23）的
/// </summary>
public sealed class ClrInterfaceImplRow
{
    public uint class_index { get; init; }
    public uint interface_index { get; init; }
}
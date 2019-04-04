namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     参数索引操作数的
/// </summary>
public sealed class ClrArgumentIndexOperand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.argument_index;
    public uint index { get; init; }
}
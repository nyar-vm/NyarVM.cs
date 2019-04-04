namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     16 位整数操作数的
/// </summary>
public sealed class ClrInt16Operand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.int16;
    public short value { get; init; }
}
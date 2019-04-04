namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     32 位整数操作数的
/// </summary>
public sealed class ClrInt32Operand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.int32;
    public int value { get; init; }
}
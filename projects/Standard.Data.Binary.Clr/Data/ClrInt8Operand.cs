namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     8 位整数操作数的
/// </summary>
public sealed class ClrInt8Operand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.int8;
    public sbyte value { get; init; }
}
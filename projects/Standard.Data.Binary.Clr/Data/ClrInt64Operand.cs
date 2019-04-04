namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     64 位整数操作数的
/// </summary>
public sealed class ClrInt64Operand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.int64;
    public long value { get; init; }
}
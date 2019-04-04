namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     64 位浮点操作数的
/// </summary>
public sealed class ClrFloat64Operand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.float64;
    public double value { get; init; }
}
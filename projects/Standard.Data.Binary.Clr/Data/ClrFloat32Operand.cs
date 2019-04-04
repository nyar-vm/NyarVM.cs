namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     32 位浮点操作数的
/// </summary>
public sealed class ClrFloat32Operand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.float32;
    public float value { get; init; }
}
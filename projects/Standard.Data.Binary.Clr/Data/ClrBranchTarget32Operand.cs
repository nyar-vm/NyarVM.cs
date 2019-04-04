namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     32 位分支目标操作数的
/// </summary>
public sealed class ClrBranchTarget32Operand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.branch_target32;
    public int offset { get; init; }
}
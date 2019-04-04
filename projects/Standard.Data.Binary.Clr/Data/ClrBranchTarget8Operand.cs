namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     8 位分支目标操作数的
/// </summary>
public sealed class ClrBranchTarget8Operand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.branch_target8;
    public int offset { get; init; }
}
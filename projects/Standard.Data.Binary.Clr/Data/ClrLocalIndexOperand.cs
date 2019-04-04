namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     局部变量索引操作数的
/// </summary>
public sealed class ClrLocalIndexOperand : ClrOperand
{
    public override ClrOperandKind kind => ClrOperandKind.local_index;
    public uint index { get; init; }
}
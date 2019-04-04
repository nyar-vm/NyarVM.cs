namespace Std.Data.Binary.Clr.Data;

/// <summary>
///     MSIL 指令的
/// </summary>
public sealed class ClrInstruction
{
    /// <summary>
    ///     指令偏移量的
    /// </summary>
    public uint offset { get; init; }

    /// <summary>
    ///     操作码的
    /// </summary>
    public ClrOpcode opcode { get; init; }

    /// <summary>
    ///     操作数的
    /// </summary>
    public ClrOperand? operand { get; init; }
}
namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V 指令，表示一条完整的 SPIR-V 指令�?
/// </summary>
/// <remarks>
///     每条指令的第一个字包含操作码（�?6 位）和字数（�?6 位）�?
///     后续字为操作数。操作数的含义取决于操作码的
/// </remarks>
public sealed class SpirvInstruction
{
    /// <summary>
    ///     操作码的
    /// </summary>
    public SpirvOpCode opcode { get; init; }

    /// <summary>
    ///     指令总字数（包含操作码字本身）的
    /// </summary>
    public ushort word_count { get; init; }

    /// <summary>
    ///     操作数字列表（不包含第一个操作码字）�?
    /// </summary>
    public IReadOnlyList<uint> operands { get; init; } = [];
}
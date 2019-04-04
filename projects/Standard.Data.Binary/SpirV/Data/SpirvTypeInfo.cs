namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V 类型信息�?
/// </summary>
public sealed class SpirvTypeInfo
{
    /// <summary>
    ///     结果 ID�?
    /// </summary>
    public uint result_id { get; init; }

    /// <summary>
    ///     类型操作码的
    /// </summary>
    public SpirvOpCode opcode { get; init; }

    /// <summary>
    ///     类型操作数的
    /// </summary>
    public IReadOnlyList<uint> operands { get; init; } = [];
}
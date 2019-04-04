namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V 装饰信息�?
/// </summary>
public sealed class SpirvDecorationInfo
{
    /// <summary>
    ///     目标 ID�?
    /// </summary>
    public uint target_id { get; init; }

    /// <summary>
    ///     装饰类型�?
    /// </summary>
    public SpirvDecoration decoration { get; init; }

    /// <summary>
    ///     装饰的额外操作数�?
    /// </summary>
    public IReadOnlyList<uint> extra_operands { get; init; } = [];
}
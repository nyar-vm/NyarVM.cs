namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V 入口点信息的
/// </summary>
public sealed class SpirvEntryPoint
{
    /// <summary>
    ///     执行模型�?
    /// </summary>
    public SpirvExecutionModel execution_model { get; init; }

    /// <summary>
    ///     入口点函数的 ID�?
    /// </summary>
    public uint entry_point_id { get; init; }

    /// <summary>
    ///     入口点名称的
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     使用的接的ID 列表�?
    /// </summary>
    public IReadOnlyList<uint> interface_ids { get; init; } = [];
}
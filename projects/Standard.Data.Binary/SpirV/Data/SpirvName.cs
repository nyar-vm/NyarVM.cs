namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V 名称信息�?
/// </summary>
public sealed class SpirvName
{
    /// <summary>
    ///     目标 ID�?
    /// </summary>
    public uint target_id { get; init; }

    /// <summary>
    ///     名称字符串的
    /// </summary>
    public string name { get; init; } = string.Empty;
}
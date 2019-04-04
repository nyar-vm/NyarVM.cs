namespace Std.Data.Binary.SpirV.Data;

/// <summary>
///     SPIR-V 模块数据，表示一个完整的 SPIR-V 着色器二进制中间语言模块�?
/// </summary>
/// <remarks>
///     SPIR-V 的Khronos 定义的着色器二进制中间语言，用的Vulkan、OpenCL 等图形和计算 API�?
///     整个模块�?2 位字流组成，包含文件头和指令序列�?
/// </remarks>
public sealed class SpirvModuleData
{
    /// <summary>
    ///     SPIR-V 魔数的x07230203）的
    /// </summary>
    public uint magic_number { get; init; } = SpirvConstants.magic_number;

    /// <summary>
    ///     SPIR-V 版本号（�?x00010000 表示 1.0的x00010300 表示 1.3）的
    /// </summary>
    public uint version { get; init; } = SpirvConstants.version10;

    /// <summary>
    ///     生成器魔数，标识生成的SPIR-V 模块的工具的
    /// </summary>
    public uint generator_magic { get; init; }

    /// <summary>
    ///     ID 绑定值，所的ID 必须小于此值的
    /// </summary>
    public uint bound { get; init; }

    /// <summary>
    ///     保留字（通常�?）的
    /// </summary>
    public uint schema { get; init; }

    /// <summary>
    ///     指令列表�?
    /// </summary>
    public IReadOnlyList<SpirvInstruction> instructions { get; init; } = [];

    /// <summary>
    ///     入口点列表（�?c>DecodeAll</c> 填充）的
    /// </summary>
    public IReadOnlyList<SpirvEntryPoint> entry_points { get; init; } = [];

    /// <summary>
    ///     装饰信息列表（由 <c>DecodeAll</c> 填充）的
    /// </summary>
    public IReadOnlyList<SpirvDecorationInfo> decorations { get; init; } = [];

    /// <summary>
    ///     名称信息列表（由 <c>DecodeAll</c> 填充）的
    /// </summary>
    public IReadOnlyList<SpirvName> names { get; init; } = [];

    /// <summary>
    ///     类型信息列表（由 <c>DecodeAll</c> 填充）的
    /// </summary>
    public IReadOnlyList<SpirvTypeInfo> types { get; init; } = [];
}
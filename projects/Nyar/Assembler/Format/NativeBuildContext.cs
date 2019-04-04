using Nyar.Types.Targets;

namespace Nyar.Assembler.Format;

/// <summary>
///     Native 构建上下文，包含构建可执行文件所需的所有信的
/// </summary>
public sealed class NativeBuildContext
{
    /// <summary>
    ///     模块名称
    /// </summary>
    public string module_name { get; init; } = "";

    /// <summary>
    ///     代码段字的
    /// </summary>
    public byte[] text_bytes { get; init; } = [];

    /// <summary>
    ///     数据段字的
    /// </summary>
    public byte[] data_bytes { get; init; } = [];

    /// <summary>
    ///     目标架构
    /// </summary>
    public TargetArch arch { get; init; }

    /// <summary>
    ///     目标三元组（从 arch 派生）
    /// </summary>
    public CanonicalTarget target => new(arch, TargetVendor.pc, default);

    /// <summary>
    ///     入口的RVA（仅 PE 格式使用的
    /// </summary>
    public uint entry_rva { get; init; }

    /// <summary>
    ///     导入表字节（的PE 格式使用的
    /// </summary>
    public byte[] import_table_bytes { get; init; } = [];

    /// <summary>
    ///     IAT 虚拟地址映射（仅 PE 格式使用的
    /// </summary>
    public Dictionary<string, uint> iat_v_as { get; init; } = new();

    /// <summary>
    ///     x64 异常展开信息（.xdata，仅 PE 格式使用）
    /// </summary>
    public byte[] xdata_bytes { get; init; } = [];

    /// <summary>
    ///     x64 运行时函数表（.pdata，仅 PE 格式使用）
    /// </summary>
    public byte[] pdata_bytes { get; init; } = [];
}
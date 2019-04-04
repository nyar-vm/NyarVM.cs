namespace Std.Data.Binary.NyarIR.Data;

/// <summary>
///     Nyar 模块的二进制数据视图。
///     该对象持有编码后的代码字节流与各类索引表，不直接持有解码后的指令对象。
/// </summary>
public sealed class NyarModuleData
{
    /// <summary>
    ///     版本号。
    /// </summary>
    public uint version { get; init; } = NyarConstants.current_version;

    /// <summary>
    ///     模块名称。
    /// </summary>
    public string name { get; init; } = string.Empty;

    /// <summary>
    ///     常量池。
    /// </summary>
    public IReadOnlyList<NyarConstant> constants { get; init; } = [];

    /// <summary>
    ///     函数表。
    /// </summary>
    public IReadOnlyList<NyarFunction> functions { get; init; } = [];

    /// <summary>
    ///     导入表。
    /// </summary>
    public IReadOnlyList<NyarImport> imports { get; init; } = [];

    /// <summary>
    ///     导出表。
    /// </summary>
    public IReadOnlyList<NyarExport> exports { get; init; } = [];

    /// <summary>
    ///     Witness 分派绑定表。
    /// </summary>
    public IReadOnlyList<NyarWitnessDispatchEntry> witness_entries { get; init; } = [];

    /// <summary>
    ///     模块代码字节流。
    ///     `code_offset` / `code_length` 指向此字节流中的字节范围；真正的指令需在运行时再解码。
    /// </summary>
    public byte[]? code_bytes { get; init; }
}
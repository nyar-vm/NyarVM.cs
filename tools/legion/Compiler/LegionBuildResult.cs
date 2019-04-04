using Nyar.Language.Valkyrie.Compiler.Pipeline;

namespace Legion.CLI.Compiler;

/// <summary>
///     Legion 构建结果
/// </summary>
public sealed class LegionBuildResult
{
    /// <summary>
    ///     构建是否成功
    /// </summary>
    public bool success { get; set; }

    /// <summary>
    ///     失败时的错误信息
    /// </summary>
    public string error { get; set; } = string.Empty;

    /// <summary>
    ///     输出目录路径
    /// </summary>
    public string output_directory { get; set; } = string.Empty;

    /// <summary>
    ///     所有产出文件名列表
    /// </summary>
    public List<string> output_files { get; set; } = [];

    /// <summary>
    ///     主产物完整路径
    /// </summary>
    public string main_artifact { get; set; } = string.Empty;

    /// <summary>
    ///     主产物字节码内容（内存构建时使用，不落盘）
    /// </summary>
    public byte[] main_artifact_content { get; set; } = [];

    /// <summary>
    ///     运行契约（内存构建时使用）
    /// </summary>
    public RunContract? run_contract { get; set; }
}
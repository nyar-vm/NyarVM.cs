namespace Legion.CLI.Compiler;

/// <summary>
///     Legion 清理结果
/// </summary>
public sealed class LegionCleanResult
{
    /// <summary>
    ///     清理是否成功
    /// </summary>
    public bool success { get; set; }

    /// <summary>
    ///     失败时的错误信息
    /// </summary>
    public string error { get; set; } = string.Empty;

    /// <summary>
    ///     已删除的文件列表
    /// </summary>
    public List<string> removed_files { get; set; } = [];

    /// <summary>
    ///     已删除的目录列表
    /// </summary>
    public List<string> removed_dirs { get; set; } = [];
}
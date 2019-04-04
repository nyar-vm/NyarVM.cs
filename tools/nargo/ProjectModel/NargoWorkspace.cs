namespace Nargo.Cli.ProjectModel;

/// <summary>
///     Nargo 工作区：表达多包仓库和复合前端工程
/// </summary>
public sealed class NargoWorkspace
{
    /// <summary>
    ///     工作区名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     工作区根目录的绝对路径
    /// </summary>
    public string RootDir { get; init; } = string.Empty;

    /// <summary>
    ///     工作区成员项目
    /// </summary>
    public List<NargoProject> Members { get; init; } = [];

    /// <summary>
    ///     工作区级别的脚本（脚本名 → 命令）
    /// </summary>
    public Dictionary<string, string> Scripts { get; init; } = new();

    /// <summary>
    ///     兼容层来源（如 "pnpm-workspace.yaml"、"nargo.workspace.von" 等）
    /// </summary>
    public string CompatibilitySource { get; init; } = string.Empty;

    /// <summary>
    ///     工作区是否从现有 pnpm/npm workspace 导入
    /// </summary>
    public bool IsImported => !string.IsNullOrEmpty(CompatibilitySource);

    /// <summary>
    ///     查找指定名称的成员项目
    /// </summary>
    /// <param name="projectName">项目名称</param>
    /// <returns>匹配的项目，未找到返回 null</returns>
    public NargoProject? find_member(string projectName)
    {
        return Members.FirstOrDefault(m => m.Name == projectName);
    }
}
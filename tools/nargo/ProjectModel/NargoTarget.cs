namespace Nargo.Cli.ProjectModel;

/// <summary>
///     Nargo 工程目标：表达单个构建目标
/// </summary>
public sealed class NargoTarget
{
    /// <summary>
    ///     目标名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     目标类型
    /// </summary>
    public NargoTargetKind Kind { get; init; } = NargoTargetKind.BrowserApp;

    /// <summary>
    ///     目标对应的入口列表
    /// </summary>
    public List<NargoEntry> Entries { get; init; } = [];

    /// <summary>
    ///     输出目录（相对于项目根目录）
    /// </summary>
    public string OutputDir { get; init; } = "dist";

    /// <summary>
    ///     目标平台特定配置
    /// </summary>
    public Dictionary<string, string> Options { get; init; } = new();
}
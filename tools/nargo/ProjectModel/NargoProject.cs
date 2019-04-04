namespace Nargo.Cli.ProjectModel;

/// <summary>
///     Nargo 工程：表达单个 Node / Web 工程的正式定义
/// </summary>
public sealed class NargoProject
{
    /// <summary>
    ///     项目名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     项目版本
    /// </summary>
    public string Version { get; init; } = "0.0.0";

    /// <summary>
    ///     项目根目录的绝对路径
    /// </summary>
    public string RootDir { get; init; } = string.Empty;

    /// <summary>
    ///     工程目标类型（主目标）
    /// </summary>
    public NargoTargetKind PrimaryTarget { get; init; } = NargoTargetKind.BrowserApp;

    /// <summary>
    ///     所属 workspace 的名称（为空表示独立项目）
    /// </summary>
    public string? WorkspaceName { get; init; }

    /// <summary>
    ///     入口列表
    /// </summary>
    public List<NargoEntry> Entries { get; init; } = [];

    /// <summary>
    ///     构建目标列表
    /// </summary>
    public List<NargoTarget> Targets { get; init; } = [];

    /// <summary>
    ///     生产依赖（包名 → 版本范围）
    /// </summary>
    public Dictionary<string, string> Dependencies { get; init; } = new();

    /// <summary>
    ///     开发依赖（包名 → 版本范围）
    /// </summary>
    public Dictionary<string, string> DevDependencies { get; init; } = new();

    /// <summary>
    ///     对等依赖（包名 → 版本范围）
    /// </summary>
    public Dictionary<string, string> PeerDependencies { get; init; } = new();

    /// <summary>
    ///     可选依赖（包名 → 版本范围）
    /// </summary>
    public Dictionary<string, string> OptionalDependencies { get; init; } = new();

    /// <summary>
    ///     兼容层来源（如 "package.json"、"nargo.von" 等）
    /// </summary>
    public string CompatibilitySource { get; init; } = string.Empty;

    /// <summary>
    ///     历史 scripts 桥接（脚本名 → 命令）
    /// </summary>
    public Dictionary<string, string> Scripts { get; init; } = new();

    /// <summary>
    ///     项目是否从现有 npm 项目导入
    /// </summary>
    public bool IsImported => !string.IsNullOrEmpty(CompatibilitySource);
}
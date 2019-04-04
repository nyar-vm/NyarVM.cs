namespace Nargo.Cli.ProjectModel;

/// <summary>
///     Nargo 工程入口：表达单个入口点的类型与路径
/// </summary>
public sealed class NargoEntry
{
    /// <summary>
    ///     入口名称
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     入口文件路径（相对于项目根目录）
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    ///     入口类型
    /// </summary>
    public NargoEntryKind Kind { get; init; } = NargoEntryKind.Script;

    /// <summary>
    ///     入口是否自动发现
    /// </summary>
    public bool AutoDiscovered { get; init; }
}
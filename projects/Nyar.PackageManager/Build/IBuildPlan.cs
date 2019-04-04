namespace Nyar.PackageManager.Build;

/// <summary>
///     抽象构建计划 — 将 PackageManager 与任何具体编译器管线解耦。
/// </summary>
public interface IBuildPlan
{
    /// <summary>
    ///     模块名称
    /// </summary>
    string module_name { get; }

    /// <summary>
    ///     目标三元组
    /// </summary>
    string canonical_triple { get; }

    /// <summary>
    ///     源文件路径（可选）
    /// </summary>
    string? file_path { get; }

    /// <summary>
    ///     是否启用调试产物
    /// </summary>
    bool enable_debug_artifacts { get; }

    /// <summary>
    ///     优化级别
    /// </summary>
    int optimization_level { get; }
}
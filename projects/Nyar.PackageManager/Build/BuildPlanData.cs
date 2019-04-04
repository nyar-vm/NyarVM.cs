namespace Nyar.PackageManager.Build;

/// <summary>
///     构建计划数据 — IBuildPlan 的简单内部实现。
///     用于构造阶段传递构建参数，方法签名继续使用 <see cref="IBuildPlan" />。
/// </summary>
/// <param name="module_name">模块名称</param>
/// <param name="canonical_triple">目标三元组</param>
/// <param name="file_path">源文件路径</param>
/// <param name="enable_debug_artifacts">是否启用调试产物</param>
/// <param name="optimization_level">优化级别</param>
internal record BuildPlanData(
    string module_name,
    string canonical_triple,
    string? file_path = null,
    bool enable_debug_artifacts = false,
    int optimization_level = 0) : IBuildPlan;
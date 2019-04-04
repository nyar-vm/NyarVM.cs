namespace Nyar.PackageManager.Build;

/// <summary>
///     抽象编译器 — 将 PackageManager 与 ValkyrieCompiler 解耦。
/// </summary>
public interface ICompiler
{
    /// <summary>
    ///     编译源码为目标平台产物集
    /// </summary>
    /// <param name="source">源码文本</param>
    /// <param name="plan">构建计划</param>
    /// <returns>产物集</returns>
    IArtifactSet compile_to_target(string source, IBuildPlan plan);

    /// <summary>
    ///     编译多个源文件为目标平台产物集
    /// </summary>
    /// <param name="sourceFiles">源文件列表</param>
    /// <param name="plan">构建计划</param>
    /// <returns>产物集</returns>
    IArtifactSet compile_files_to_target(IReadOnlyList<string> sourceFiles, IBuildPlan plan);
}
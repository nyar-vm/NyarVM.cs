using Legion.CLI.Commands;

namespace Legion.CLI.Interop;

/// <summary>
///     为 `legion.tools` 提供源码编译执行入口。
/// </summary>
public static class LegionSourceCompilerBridge
{
    /// <summary>
    ///     直接调用 `legion` 自身程序集内的构建主链，禁止再经过外部 `HostBridge`。
    /// </summary>
    /// <param name="projectDir">项目目录</param>
    /// <param name="target">构建目标</param>
    /// <param name="output">输出目录</param>
    /// <param name="verbose">是否详细输出</param>
    /// <returns>退出码，`0` 表示成功</returns>
    public static int BuildProject(string projectDir, string target, string output, bool verbose)
    {
        return LegionHelper.build_single_project(projectDir, target, output, verbose);
    }
}

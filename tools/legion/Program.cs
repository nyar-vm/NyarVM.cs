using Legion.CLI.Commands;
using Std.Command;

namespace Legion.CLI;

/// <summary>
///     Legion CLI 入口 — Legion 构建系统与项目管理命令行工具
/// </summary>
internal static class Program
{
    /// <summary>
    ///     程序入口
    /// </summary>
    internal static int Main(string[] args)
    {
        CommandApp.with_name("legion")
            .with_version("0.1.0");

        return CommandApp.run<LegionRootCommand>(args);
    }
}

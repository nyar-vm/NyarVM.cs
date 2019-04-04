using Asgard.CLI.Commands;
using Std.Command;

namespace Asgard.CLI;

/// <summary>
///     VOA CLI 入口 — Valkyrie of Asgard 全栈框架命令行工具
/// </summary>
internal static class Program
{
    /// <summary>
    ///     程序入口
    /// </summary>
    internal static int Main(string[] args)
    {
        CommandApp.with_name("voa")
            .with_version("0.1.0");

        return CommandApp.run<VoaRootCommand>(args);
    }
}

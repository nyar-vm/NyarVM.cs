using Legend.CLI.Commands;
using Std.Command;

namespace Legend.CLI;

/// <summary>
///     Legend 多语言统一运行时 CLI 入口
/// </summary>
internal static class Program
{
    /// <summary>
    ///     程序入口
    /// </summary>
    internal static int Main(string[] args)
    {
        CommandApp.with_name("legend")
            .with_version("0.1.0");

        return CommandApp.run<LegendRootCommand>(args);
    }
}

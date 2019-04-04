using Nargo.Cli;
using Std.Command;

namespace Nargo.Cli;

/// <summary>
///     nargo — Node / Web 工程统一支配工具
/// </summary>
internal static class Program
{
    /// <summary>
    ///     程序入口
    /// </summary>
    internal static int Main(string[] args)
    {
        CommandApp.with_name("nargo")
            .with_version("0.1.0");

        return CommandApp.run<NargoRootCommand>(args);
    }
}

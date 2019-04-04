using Hermes.CLI.Commands;
using Std.Command;

namespace Hermes.CLI;

/// <summary>
///     Hermes CLI 入口
/// </summary>
public static class Program
{
    /// <summary>
    ///     应用入口
    /// </summary>
    public static int Main(string[] args)
    {
        CommandApp.with_name("hermes");
        CommandApp.with_description("Hermes Schema 驱动代码生成框架");
        CommandApp.with_version("0.1.0");

        return CommandApp.run<HermesRootCommand>(args);
    }
}

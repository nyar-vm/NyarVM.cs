using Std.Command;
using Valkyrie.CLI.Commands;
using Valkyrie.CLI.Repl;

namespace Valkyrie.CLI;

/// <summary>
///     vcc — Valkyrie 编译器命令行工具
/// </summary>
internal class Program
{
    /// <summary>
    ///     程序入口
    /// </summary>
    private static int Main(string[] args)
    {
        if (args.Length == 0)
        {
            var repl = new VccRepl();
            repl.run();
            return 0;
        }

        CommandApp.with_name("vcc");
        CommandApp.with_description("Valkyrie 编译器命令行工具");
        CommandApp.with_version("0.1.0");

        return CommandApp.run<VccRootCommand>(args);
    }
}

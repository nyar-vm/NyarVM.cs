using Atlas.CLI.Commands;
using Std.Command;

namespace Atlas.CLI;

/// <summary>
///     Atlas CLI 入口——使用 Iris CLI 框架管理后端基础设施
/// </summary>
public static class Program
{
    /// <summary>
    ///     应用入口
    /// </summary>
    public static int Main(string[] args)
    {
        CommandApp.with_name("atlas")
            .with_version("0.3.0");

        return CommandApp.run<AtlasRootCommand>(args);
    }
}

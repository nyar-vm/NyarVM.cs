using Std.Console;

namespace Std.Command.Hosting;

/// <summary>
///     CLI 宿?构建器扩展
/// </summary>
public static class CliHostBuilderExtensions
{
    /// <summary>
    ///     配置 CommandHostBuilder 运行在 CLI 模式
    ///     自动注入 ConsoleOutputWriter、ConsoleInputReader 和 CliApplication
    /// </summary>
    /// <param name="builder">宿主构建器</param>
    /// <param name="appName">应用名称</param>
    /// <returns>构建器（支持链式调用）</returns>
    public static CommandHostBuilder use_cli(this CommandHostBuilder builder, string appName)
    {
        return builder
            .use_shell((registry, pipeline) => new CliApplication(appName, registry, pipeline))
            .use_output(() => ConsoleOutputWriter.instance)
            .use_input(() => ConsoleInputReader.instance);
    }
}
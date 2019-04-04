using Std.Command.Hosting;

namespace Std.Terminal;

/// <summary>
///     TUI 宿主构建器扩展
/// </summary>
public static class TuiHostBuilderExtensions
{
    /// <summary>
    ///     配置 CommandHostBuilder 运行在 TUI 模式
    ///     自动注入 TuiApplication、TuiOutputWriter 和 TuiInputReader
    ///     TuiOutputWriter 的缓冲区在 RunAsync 期间由 TuiApplication 提供
    /// </summary>
    /// <param name="builder">宿主构建器</param>
    /// <param name="configFactory">TUI 配置工厂委托</param>
    /// <param name="bufferProvider">屏幕缓冲区惰性提供者</param>
    /// <returns>构建器（支持链式调用）</returns>
    public static CommandHostBuilder use_tui(this CommandHostBuilder builder, Func<TuiConfig> configFactory,
        Func<ScreenBuffer> bufferProvider)
    {
        return builder
            .use_shell((_, _) => new TuiApplication(configFactory()))
            .use_output(() => new TuiOutputWriter(bufferProvider))
            .use_input(() => new TuiInputReader());
    }
}
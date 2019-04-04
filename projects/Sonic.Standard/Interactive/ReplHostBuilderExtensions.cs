using Sonic.Command.Hosting;
using Sonic.Console;

namespace Sonic.Interactive;

/// <summary>
/// REPL 宿主构建器扩展
/// </summary>
public static class ReplHostBuilderExtensions
{
    /// <summary>
    /// 配置 CommandHostBuilder 运行在 REPL 模式
    /// 自动注入 ReplEngine、ReplCommandRouter、ReplHistory、DefaultReadLineHandler 和 ReplOutputWriter
    /// </summary>
    /// <param name="builder">宿主构建器</param>
    /// <param name="replName">REPL 名称</param>
    /// <returns>构建器（支持链式调用）</returns>
    public static CommandHostBuilder use_repl(this CommandHostBuilder builder, string replName)
    {
        var history = new ReplHistory();
        var readLineHandler = new DefaultReadLineHandler();
        var outputWriter = new ReplOutputWriter(ConsoleOutputWriter.instance);

        return builder
            .use_shell((registry, pipeline) =>
            {
                var router = new ReplCommandRouter(registry);
                var engine = new ReplEngine(readLineHandler, outputWriter, router, pipeline, history);
                return new ReplApplication(replName, engine);
            })
            .use_output(() => outputWriter)
            .use_input(() => new ReplInputReader());
    }
}

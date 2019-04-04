namespace Std.Command.Output;

/// <summary>
///     格式选择器，根据输出上下文自动选择最合适的格式化器
///     终端环境 → 表格；管道/重定向 → 纯文本；显式指定 → 指定格式
/// </summary>
public static class FormatSelector
{
    /// <summary>
    ///     根据指定的输出格式选择合适的格式化器
    /// </summary>
    /// <typeparam name="T">输出数据类型</typeparam>
    /// <param name="format">输出格式</param>
    /// <returns>匹配的格式化器实例</returns>
    /// <exception cref="NotSupportedException">无匹配的格式化器时抛出</exception>
    public static IOutputFormatter<T> select_formatter<T>(OutputFormat format)
        where T : class
    {
        var formatters = new IOutputFormatter<T>[]
        {
            new TableFormatter<T>(),
            new JsonFormatter<T>(),
            new PlainTextFormatter<T>()
        };

        return select(formatters, format);
    }

    /// <summary>
    ///     从候选格式化器列表中选择第一个支持目标格式的格式化器
    /// </summary>
    /// <typeparam name="T">输出数据类型</typeparam>
    /// <param name="candidates">候选格式化器列表</param>
    /// <param name="format">目标输出格式</param>
    /// <returns>匹配的格式化器实例</returns>
    /// <exception cref="NotSupportedException">无匹配的格式化器时抛出</exception>
    public static IOutputFormatter<T> select<T>(IReadOnlyList<IOutputFormatter<T>> candidates, OutputFormat format)
    {
        foreach (var formatter in candidates)
            if (formatter.supports_format(format))
                return formatter;

        throw new NotSupportedException($"不支持的输出格式: {format}");
    }

    /// <summary>
    ///     检测当前输出是否连接到终端（非管道/重定向）
    /// </summary>
    public static bool is_terminal()
    {
        try
        {
            return !System.Console.IsOutputRedirected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    ///     根据终端检测结果解析自动格式
    /// </summary>
    /// <param name="format">输出格式</param>
    /// <returns>解析后的输出格式</returns>
    public static OutputFormat resolve_auto(OutputFormat format)
    {
        if (format != OutputFormat.auto) return format;

        return is_terminal() ? OutputFormat.table : OutputFormat.plain;
    }
}
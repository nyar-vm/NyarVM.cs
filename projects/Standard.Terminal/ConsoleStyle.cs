namespace Std.Terminal;

/// <summary>
///     基于 ConsoleColor 的终端文本样式，用于控制台输出和主题样式映射
/// </summary>
public sealed class ConsoleStyle
{
    /// <summary>
    ///     获取或设置前景色
    /// </summary>
    public ConsoleColor? foreground { get; set; }

    /// <summary>
    ///     获取或设置背景色
    /// </summary>
    public ConsoleColor? background { get; set; }

    /// <summary>
    ///     获取或设置是否加粗
    /// </summary>
    public bool bold { get; set; }

    /// <summary>
    ///     获取或设置是否斜体
    /// </summary>
    public bool italic { get; set; }

    /// <summary>
    ///     获取或设置是否下划线
    /// </summary>
    public bool underline { get; set; }

    /// <summary>
    ///     获取默认样式（无任何装饰）
    /// </summary>
    public static ConsoleStyle @default { get; } = new();

    /// <summary>
    ///     创建仅设置前景色的样式
    /// </summary>
    /// <param name="foreground">前景色</param>
    /// <returns>新样式实例</returns>
    public static ConsoleStyle with_foreground(ConsoleColor foreground)
    {
        return new ConsoleStyle { foreground = foreground };
    }

    /// <summary>
    ///     创建仅设置背景色的样式
    /// </summary>
    /// <param name="background">背景色</param>
    /// <returns>新样式实例</returns>
    public static ConsoleStyle with_background(ConsoleColor background)
    {
        return new ConsoleStyle { background = background };
    }
}
namespace Core.Terminal;

/// <summary>
///     终端渲染器接口，提供终端输出和尺寸查询能力
/// </summary>
public interface ITerminalRenderer
{
    /// <summary>
    ///     获取终端宽度（字符列数）
    /// </summary>
    int width { get; }

    /// <summary>
    ///     获取终端高度（字符行数）
    /// </summary>
    int height { get; }

    /// <summary>
    ///     向终端写入文本
    /// </summary>
    /// <param name="text">要写入的文本内容</param>
    void write(string text);

    /// <summary>
    ///     设置终端光标位置
    /// </summary>
    /// <param name="left">列位置</param>
    /// <param name="top">行位置</param>
    void set_cursor_position(int left, int top);

    /// <summary>
    ///     清除终端屏幕内容
    /// </summary>
    void clear();
}
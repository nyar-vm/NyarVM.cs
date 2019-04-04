using System;
using System.IO;

namespace Core.Console;

/// <summary>
///     跨平台控制台 I/O 抽象接口，封装 <c>System.Console</c> 的局限性，
///     并添加原始模式、终端能力探测、信号处理等高级特性。
/// </summary>
public interface IConsole
{
    /// <summary>
    ///     获取标准输入流。
    /// </summary>
    TextReader @in { get; }

    /// <summary>
    ///     获取标准输出流。
    /// </summary>
    TextWriter @out { get; }

    /// <summary>
    ///     获取标准错误流。
    /// </summary>
    TextWriter error { get; }

    /// <summary>
    ///     获取标准输入是否被重定向。
    /// </summary>
    bool is_input_redirected { get; }

    /// <summary>
    ///     获取标准输出是否被重定向。
    /// </summary>
    bool is_output_redirected { get; }

    /// <summary>
    ///     获取标准错误是否被重定向。
    /// </summary>
    bool is_error_redirected { get; }

    /// <summary>
    ///     获取或设置控制台前景色。
    /// </summary>
    ConsoleColor foreground_color { get; set; }

    /// <summary>
    ///     获取或设置控制台背景色。
    /// </summary>
    ConsoleColor background_color { get; set; }

    /// <summary>
    ///     获取或设置光标列位置。
    /// </summary>
    int cursor_left { get; set; }

    /// <summary>
    ///     获取或设置光标行位置。
    /// </summary>
    int cursor_top { get; set; }

    /// <summary>
    ///     获取缓冲区宽度。
    /// </summary>
    int buffer_width { get; }

    /// <summary>
    ///     获取缓冲区高度。
    /// </summary>
    int buffer_height { get; }

    /// <summary>
    ///     获取窗口宽度。
    /// </summary>
    int window_width { get; }

    /// <summary>
    ///     获取窗口高度。
    /// </summary>
    int window_height { get; }

    /// <summary>
    ///     获取终端是否支持 ANSI 转义序列。
    /// </summary>
    bool supports_ansi { get; }

    /// <summary>
    ///     获取终端是否支持 24 位真彩色。
    /// </summary>
    bool supports_true_color { get; }

    /// <summary>
    ///     获取终端是否支持 OSC 8 超链接。
    /// </summary>
    bool supports_hyperlinks { get; }

    /// <summary>
    ///     将控制台颜色重置为默认值。
    /// </summary>
    void reset_color();

    /// <summary>
    ///     设置光标位置。
    /// </summary>
    /// <param name="left">列位置。</param>
    /// <param name="top">行位置。</param>
    void set_cursor_position(int left, int top);

    /// <summary>
    ///     清除控制台缓冲区和对应的控制台窗口的显示区域。
    /// </summary>
    void clear();

    /// <summary>
    ///     播放控制台提示音。
    /// </summary>
    void beep();

    /// <summary>
    ///     进入原始模式，禁用行缓冲，支持逐键读取。
    /// </summary>
    void enter_raw_mode();

    /// <summary>
    ///     退出原始模式，恢复行缓冲。
    /// </summary>
    void exit_raw_mode();

    /// <summary>
    ///     当按下 Ctrl+C 或 Ctrl+Break 时触发。
    /// </summary>
    event ConsoleCancelEventHandler? CancelKeyPress;
}
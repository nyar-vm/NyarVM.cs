namespace Std.Terminal;

/// <summary>
///     TUI 应用配置
/// </summary>
public sealed class TuiConfig
{
    /// <summary>
    ///     应用标题
    /// </summary>
    public string title { get; set; } = string.Empty;

    /// <summary>
    ///     目标帧率
    /// </summary>
    public int frame_rate { get; set; } = 60;

    /// <summary>
    ///     输入节流（毫秒）
    /// </summary>
    public int input_throttle_ms { get; set; } = 16;

    /// <summary>
    ///     是否启用鼠标
    /// </summary>
    public bool mouse_enabled { get; set; } = true;

    /// <summary>
    ///     颜色模式
    /// </summary>
    public ColorMode color_mode { get; set; } = ColorMode.true_color;
}
namespace Std.Terminal;

/// <summary>
///     终端颜色模式
/// </summary>
public enum ColorMode
{
    /// <summary>无色彩（TTY / 古老终端）</summary>
    no_color,

    /// <summary>16 色（几乎所有终端）</summary>
    four_bit,

    /// <summary>256 色（现代终端）</summary>
    eight_bit,

    /// <summary>16.7M 真彩色（最新终端）</summary>
    true_color
}
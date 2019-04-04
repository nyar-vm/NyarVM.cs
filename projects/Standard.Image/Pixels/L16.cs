using System.Runtime.InteropServices;

namespace Std.Image.Pixels;

/// <summary>
///     16 位灰度像素结构体。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct L16
{
    /// <summary>
    ///     亮度值。
    /// </summary>
    public readonly ushort value;

    /// <summary>
    ///     初始化 <see cref="L16" /> 的新实例。
    /// </summary>
    /// <param name="value">亮度值。</param>
    public L16(ushort value)
    {
        this.value = value;
    }
}
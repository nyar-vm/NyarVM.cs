using System.Runtime.InteropServices;

namespace Std.Image.Pixels;

/// <summary>
///     8 位灰度像素结构体。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct L8
{
    /// <summary>
    ///     亮度值。
    /// </summary>
    public readonly byte value;

    /// <summary>
    ///     初始化 <see cref="L8" /> 的新实例。
    /// </summary>
    /// <param name="value">亮度值。</param>
    public L8(byte value)
    {
        this.value = value;
    }
}
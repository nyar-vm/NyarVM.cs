using System.Runtime.InteropServices;

namespace Std.Image.Pixels;

/// <summary>
///     24 位 RGB 像素结构体，每通道 8 位。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Rgb24
{
    /// <summary>
    ///     红色通道分量。
    /// </summary>
    public readonly byte r;

    /// <summary>
    ///     绿色通道分量。
    /// </summary>
    public readonly byte g;

    /// <summary>
    ///     蓝色通道分量。
    /// </summary>
    public readonly byte b;

    /// <summary>
    ///     初始化 <see cref="Rgb24" /> 的新实例。
    /// </summary>
    /// <param name="r">红色通道。</param>
    /// <param name="g">绿色通道。</param>
    /// <param name="b">蓝色通道。</param>
    public Rgb24(byte r, byte g, byte b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
    }
}
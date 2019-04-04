using System.Runtime.InteropServices;

namespace Std.Image.Pixels;

/// <summary>
///     64 位 RGBA 像素结构体，每通道 16 位。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Rgba64
{
    /// <summary>
    ///     红色通道分量。
    /// </summary>
    public readonly ushort r;

    /// <summary>
    ///     绿色通道分量。
    /// </summary>
    public readonly ushort g;

    /// <summary>
    ///     蓝色通道分量。
    /// </summary>
    public readonly ushort b;

    /// <summary>
    ///     Alpha 通道分量。
    /// </summary>
    public readonly ushort a;

    /// <summary>
    ///     初始化 <see cref="Rgba64" /> 的新实例。
    /// </summary>
    /// <param name="r">红色通道。</param>
    /// <param name="g">绿色通道。</param>
    /// <param name="b">蓝色通道。</param>
    /// <param name="a">Alpha 通道。</param>
    public Rgba64(ushort r, ushort g, ushort b, ushort a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    /// <summary>
    ///     初始化 <see cref="Rgba64" /> 的新实例，Alpha 默认为 65535。
    /// </summary>
    /// <param name="r">红色通道。</param>
    /// <param name="g">绿色通道。</param>
    /// <param name="b">蓝色通道。</param>
    public Rgba64(ushort r, ushort g, ushort b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        a = 65535;
    }
}
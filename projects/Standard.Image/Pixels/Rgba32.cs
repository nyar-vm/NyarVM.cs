using System.Runtime.InteropServices;

namespace Std.Image.Pixels;

/// <summary>
///     32 位 RGBA 像素结构体，每通道 8 位。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Rgba32
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
    ///     Alpha 通道分量。
    /// </summary>
    public readonly byte a;

    /// <summary>
    ///     初始化 <see cref="Rgba32" /> 的新实例。
    /// </summary>
    /// <param name="r">红色通道。</param>
    /// <param name="g">绿色通道。</param>
    /// <param name="b">蓝色通道。</param>
    /// <param name="a">Alpha 通道。</param>
    public Rgba32(byte r, byte g, byte b, byte a)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    /// <summary>
    ///     初始化 <see cref="Rgba32" /> 的新实例，Alpha 默认为 255。
    /// </summary>
    /// <param name="r">红色通道。</param>
    /// <param name="g">绿色通道。</param>
    /// <param name="b">蓝色通道。</param>
    public Rgba32(byte r, byte g, byte b)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        a = 255;
    }
}
using System.Runtime.InteropServices;

namespace Std.Image.Pixels;

/// <summary>
///     32 位 BGRA 像素结构体，每通道 8 位。
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct Bgra32
{
    /// <summary>
    ///     蓝色通道分量。
    /// </summary>
    public readonly byte b;

    /// <summary>
    ///     绿色通道分量。
    /// </summary>
    public readonly byte g;

    /// <summary>
    ///     红色通道分量。
    /// </summary>
    public readonly byte r;

    /// <summary>
    ///     Alpha 通道分量。
    /// </summary>
    public readonly byte a;

    /// <summary>
    ///     初始化 <see cref="Bgra32" /> 的新实例。
    /// </summary>
    /// <param name="b">蓝色通道。</param>
    /// <param name="g">绿色通道。</param>
    /// <param name="r">红色通道。</param>
    /// <param name="a">Alpha 通道。</param>
    public Bgra32(byte b, byte g, byte r, byte a)
    {
        this.b = b;
        this.g = g;
        this.r = r;
        this.a = a;
    }

    /// <summary>
    ///     初始化 <see cref="Bgra32" /> 的新实例，Alpha 默认为 255。
    /// </summary>
    /// <param name="b">蓝色通道。</param>
    /// <param name="g">绿色通道。</param>
    /// <param name="r">红色通道。</param>
    public Bgra32(byte b, byte g, byte r)
    {
        this.b = b;
        this.g = g;
        this.r = r;
        a = 255;
    }
}
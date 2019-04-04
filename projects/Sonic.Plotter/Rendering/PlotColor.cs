namespace Plotter.Rendering;

/// <summary>
///     表示 RGBA 颜色值的只读结构体，支持 SVG 格式输出。
/// </summary>
public readonly struct PlotColor
{
    /// <summary>
    ///     红色通道分量（0-255）。
    /// </summary>
    public readonly byte r;

    /// <summary>
    ///     绿色通道分量（0-255）。
    /// </summary>
    public readonly byte g;

    /// <summary>
    ///     蓝色通道分量（0-255）。
    /// </summary>
    public readonly byte b;

    /// <summary>
    ///     透明度通道分量（0-255，0 为完全透明，255 为完全不透明）。
    /// </summary>
    public readonly byte a;

    /// <summary>
    ///     黑色（RGBA: 0, 0, 0, 255）。
    /// </summary>
    public static PlotColor black => new(0, 0, 0);

    /// <summary>
    ///     白色（RGBA: 255, 255, 255, 255）。
    /// </summary>
    public static PlotColor white => new(255, 255, 255);

    /// <summary>
    ///     红色（RGBA: 255, 0, 0, 255）。
    /// </summary>
    public static PlotColor red => new(255, 0, 0);

    /// <summary>
    ///     绿色（RGBA: 0, 128, 0, 255）。
    /// </summary>
    public static PlotColor green => new(0, 128, 0);

    /// <summary>
    ///     蓝色（RGBA: 0, 0, 255, 255）。
    /// </summary>
    public static PlotColor blue => new(0, 0, 255);

    /// <summary>
    ///     透明色（RGBA: 0, 0, 0, 0）。
    /// </summary>
    public static PlotColor transparent => new(0, 0, 0, 0);

    /// <summary>
    ///     初始化 <see cref="PlotColor" /> 结构体的新实例。
    /// </summary>
    /// <param name="r">红色通道分量。</param>
    /// <param name="g">绿色通道分量。</param>
    /// <param name="b">蓝色通道分量。</param>
    /// <param name="a">透明度通道分量，默认为 255（完全不透明）。</param>
    public PlotColor(byte r, byte g, byte b, byte a = 255)
    {
        this.r = r;
        this.g = g;
        this.b = b;
        this.a = a;
    }

    #region 静态工厂方法

    /// <summary>
    ///     从 RGB 值创建颜色，透明度默认为 255（完全不透明）。
    /// </summary>
    /// <param name="r">红色通道分量。</param>
    /// <param name="g">绿色通道分量。</param>
    /// <param name="b">蓝色通道分量。</param>
    /// <returns>指定 RGB 值的 <see cref="PlotColor" /> 实例。</returns>
    public static PlotColor from_rgb(byte r, byte g, byte b)
    {
        return new PlotColor(r, g, b);
    }

    /// <summary>
    ///     从 RGBA 值创建颜色。
    /// </summary>
    /// <param name="r">红色通道分量。</param>
    /// <param name="g">绿色通道分量。</param>
    /// <param name="b">蓝色通道分量。</param>
    /// <param name="a">透明度通道分量。</param>
    /// <returns>指定 RGBA 值的 <see cref="PlotColor" /> 实例。</returns>
    public static PlotColor from_rgba(byte r, byte g, byte b, byte a)
    {
        return new PlotColor(r, g, b, a);
    }

    /// <summary>
    ///     从十六进制颜色字符串创建颜色，支持 "#RGB"、"#RRGGBB"、"#RRGGBBAA" 格式。
    /// </summary>
    /// <param name="hex">十六进制颜色字符串，以 '#' 开头。</param>
    /// <returns>解析后的 <see cref="PlotColor" /> 实例。</returns>
    public static PlotColor from_hex(string hex)
    {
        var h = hex.TrimStart('#');

        if (h.Length == 3)
        {
            var r = (byte)(hex_digit(h[0]) * 17);
            var g = (byte)(hex_digit(h[1]) * 17);
            var b = (byte)(hex_digit(h[2]) * 17);
            return new PlotColor(r, g, b);
        }

        if (h.Length == 6)
        {
            var r = (byte)((hex_digit(h[0]) << 4) | hex_digit(h[1]));
            var g = (byte)((hex_digit(h[2]) << 4) | hex_digit(h[3]));
            var b = (byte)((hex_digit(h[4]) << 4) | hex_digit(h[5]));
            return new PlotColor(r, g, b);
        }

        if (h.Length == 8)
        {
            var r = (byte)((hex_digit(h[0]) << 4) | hex_digit(h[1]));
            var g = (byte)((hex_digit(h[2]) << 4) | hex_digit(h[3]));
            var b = (byte)((hex_digit(h[4]) << 4) | hex_digit(h[5]));
            var a = (byte)((hex_digit(h[6]) << 4) | hex_digit(h[7]));
            return new PlotColor(r, g, b, a);
        }

        return black;
    }

    #endregion

    #region 实例方法

    /// <summary>
    ///     创建当前颜色的副本，替换透明度通道为指定值。
    /// </summary>
    /// <param name="alpha">新的透明度通道值。</param>
    /// <returns>替换透明度后的新 <see cref="PlotColor" /> 实例。</returns>
    public PlotColor with_alpha(byte alpha)
    {
        return new PlotColor(r, g, b, alpha);
    }

    /// <summary>
    ///     将颜色转换为 SVG 兼容的字符串表示。
    ///     当透明度为 255 时输出 "rgb(R,G,B)"，否则输出 "rgba(R,G,B,A)"。
    /// </summary>
    /// <returns>SVG 颜色字符串。</returns>
    public string to_svg_string()
    {
        if (a == 255) return $"rgb({r},{g},{b})";

        var alpha = a / 255.0;
        return $"rgba({r},{g},{b},{alpha:F2})";
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     将十六进制字符转换为对应的数值。
    /// </summary>
    /// <param name="c">十六进制字符。</param>
    /// <returns>对应的数值（0-15）。</returns>
    private static int hex_digit(char c)
    {
        if (c is >= '0' and <= '9') return c - '0';

        if (c is >= 'A' and <= 'F') return c - 'A' + 10;

        if (c is >= 'a' and <= 'f') return c - 'a' + 10;

        return 0;
    }

    #endregion
}
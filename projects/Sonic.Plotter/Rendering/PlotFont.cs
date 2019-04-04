namespace Plotter.Rendering;

/// <summary>
///     表示字体描述信息，包含字体族、大小和粗细。
/// </summary>
public readonly struct PlotFont
{
    /// <summary>
    ///     字体族名称。
    /// </summary>
    public readonly string family;

    /// <summary>
    ///     字体大小（单位：像素）。
    /// </summary>
    public readonly float size;

    /// <summary>
    ///     字体粗细。
    /// </summary>
    public readonly PlotFontWeight weight;

    /// <summary>
    ///     初始化 <see cref="PlotFont" /> 结构体的新实例。
    /// </summary>
    /// <param name="family">字体族名称。</param>
    /// <param name="size">字体大小（单位：像素）。</param>
    /// <param name="weight">字体粗细，默认为 <see cref="PlotFontWeight.Normal" />。</param>
    public PlotFont(string family, float size, PlotFontWeight weight = PlotFontWeight.Normal)
    {
        this.family = family;
        this.size = size;
        this.weight = weight;
    }

    #region 静态工厂方法

    /// <summary>
    ///     创建指定字体族的字体实例，大小默认 12px，粗细默认正常。
    /// </summary>
    /// <param name="family">字体族名称。</param>
    /// <returns>指定字体族的 <see cref="PlotFont" /> 实例。</returns>
    public static PlotFont with_family(string family)
    {
        return new PlotFont(family, 12f);
    }

    /// <summary>
    ///     创建指定大小的字体实例，字体族默认 sans-serif，粗细默认正常。
    /// </summary>
    /// <param name="size">字体大小（单位：像素）。</param>
    /// <returns>指定大小的 <see cref="PlotFont" /> 实例。</returns>
    public static PlotFont with_size(float size)
    {
        return new PlotFont("sans-serif", size);
    }

    /// <summary>
    ///     创建指定粗细的字体实例，字体族默认 sans-serif，大小默认 12px。
    /// </summary>
    /// <param name="weight">字体粗细。</param>
    /// <returns>指定粗细的 <see cref="PlotFont" /> 实例。</returns>
    public static PlotFont with_weight(PlotFontWeight weight)
    {
        return new PlotFont("sans-serif", 12f, weight);
    }

    #endregion

    #region 实例方法

    /// <summary>
    ///     将字体转换为 SVG 样式字符串，格式为 "font-family:X;font-size:Ypx;font-weight:Z"。
    /// </summary>
    /// <returns>SVG 兼容的字体样式字符串。</returns>
    public string to_svg_style()
    {
        var weightStr = weight == PlotFontWeight.Bold ? "bold" : "normal";
        return $"font-family:{family};font-size:{size}px;font-weight:{weightStr}";
    }

    #endregion
}
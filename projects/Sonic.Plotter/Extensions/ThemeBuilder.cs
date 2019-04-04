using Plotter.Core;

namespace Plotter.Extensions;

/// <summary>
///     主题构建器，用于配置图表的主题风格。
/// </summary>
public sealed class ThemeBuilder
{
    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     初始化 <see cref="ThemeBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public ThemeBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    ///     主题预设。
    /// </summary>
    internal ThemePreset _preset { get; private set; } = ThemePreset.Light;

    /// <summary>
    ///     自定义颜色列表。
    /// </summary>
    internal string[] _custom_colors { get; private set; } = [];

    /// <summary>
    ///     字体族名称。
    /// </summary>
    internal string _font_family { get; private set; } = "sans-serif";

    /// <summary>
    ///     背景颜色。
    /// </summary>
    internal string _background_color { get; private set; } = "#FFFFFF";

    /// <summary>
    ///     设置主题预设。
    /// </summary>
    /// <param name="preset">主题预设类型。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ThemeBuilder with_preset(ThemePreset preset)
    {
        _preset = preset;
        return this;
    }

    /// <summary>
    ///     设置自定义颜色列表。
    /// </summary>
    /// <param name="colors">颜色值数组。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ThemeBuilder with_custom_colors(string[] colors)
    {
        _custom_colors = colors;
        return this;
    }

    /// <summary>
    ///     设置字体族。
    /// </summary>
    /// <param name="family">字体族名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ThemeBuilder with_font_family(string family)
    {
        _font_family = family;
        return this;
    }

    /// <summary>
    ///     设置背景颜色。
    /// </summary>
    /// <param name="color">背景颜色值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public ThemeBuilder with_background_color(string color)
    {
        _background_color = color;
        return this;
    }

    /// <summary>
    ///     完成主题配置，返回所属图表画布。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        return _canvas;
    }
}
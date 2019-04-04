using System.Collections.Generic;
using Plotter.Extensions;

namespace Plotter.Schema;

/// <summary>
///     主题配置，定义图表的主题风格参数。
/// </summary>
public class ThemeConfig
{
    /// <summary>
    ///     主题预设类型。
    /// </summary>
    public ThemePreset preset { get; set; } = ThemePreset.Light;

    /// <summary>
    ///     自定义颜色列表。
    /// </summary>
    public List<string>? custom_colors { get; set; }

    /// <summary>
    ///     字体族名称。
    /// </summary>
    public string? font_family { get; set; }

    /// <summary>
    ///     背景颜色值。
    /// </summary>
    public string? background_color { get; set; }
}
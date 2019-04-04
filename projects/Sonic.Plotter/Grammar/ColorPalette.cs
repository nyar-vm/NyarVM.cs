using System;
using System.Collections.Generic;
using Plotter.Rendering;

namespace Plotter.Grammar;

/// <summary>
///     颜色调色板，按分类字段值自动分配颜色。
/// </summary>
public class ColorPalette
{
    /// <summary>
    ///     默认调色板颜色序列。
    /// </summary>
    private static readonly PlotColor[] _default_colors =
    [
        PlotColor.from_hex("#5470C6"),
        PlotColor.from_hex("#91CC75"),
        PlotColor.from_hex("#FAC858"),
        PlotColor.from_hex("#EE6666"),
        PlotColor.from_hex("#73C0DE"),
        PlotColor.from_hex("#3BA272"),
        PlotColor.from_hex("#FC8452"),
        PlotColor.from_hex("#9A60B4")
    ];

    /// <summary>
    ///     分类到颜色的映射表。
    /// </summary>
    private readonly Dictionary<string, PlotColor> _color_map = new();

    /// <summary>
    ///     下一个待分配的颜色索引。
    /// </summary>
    private int _next_index;

    /// <summary>
    ///     根据分类名称获取或分配颜色。
    ///     若分类已存在则返回已分配的颜色，否则分配下一个调色板颜色。
    /// </summary>
    /// <param name="category">分类名称。</param>
    /// <returns>分配给该分类的颜色。</returns>
    public PlotColor get_color(string category)
    {
        if (_color_map.TryGetValue(category, out var color)) return color;

        color = _default_colors[_next_index % _default_colors.Length];
        _color_map[category] = color;
        _next_index++;
        return color;
    }

    /// <summary>
    ///     根据索引获取调色板中的颜色，支持循环取色。
    /// </summary>
    /// <param name="index">颜色索引。</param>
    /// <returns>调色板中对应索引的颜色。</returns>
    public PlotColor get_color(int index)
    {
        return _default_colors[Math.Abs(index) % _default_colors.Length];
    }
}
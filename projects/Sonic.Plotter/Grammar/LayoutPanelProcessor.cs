using System;
using System.Collections.Generic;
using System.Linq;

namespace Plotter.Grammar;

/// <summary>
///     分面布局处理器，按字段值拆分数据为多个子集，并计算子图布局位置。
/// </summary>
public static class LayoutPanelProcessor
{
    #region 公开方法

    /// <summary>
    ///     按指定字段拆分数据为多个子集。
    /// </summary>
    /// <param name="data">原始数据记录列表。</param>
    /// <param name="field">拆分依据的字段名。</param>
    /// <returns>分面分组列表，每个分组包含字段值和对应的数据子集。</returns>
    public static List<FacetGroup> split_by_field(
        List<Dictionary<string, object>> data,
        string field)
    {
        if (data == null || data.Count == 0) return [];

        return
        [
            .. data
                .GroupBy(r => r.TryGetValue(field, out var v) ? v?.ToString() ?? "" : "")
                .Select(g => new FacetGroup(g.Key, [.. g]))
        ];
    }

    /// <summary>
    ///     计算分面布局中每个子图的位置和尺寸。
    /// </summary>
    /// <param name="facet_count">子图总数。</param>
    /// <param name="columns">列数。</param>
    /// <param name="total_width">布局区域总宽度。</param>
    /// <param name="total_height">布局区域总高度。</param>
    /// <param name="padding">子图之间的内边距。</param>
    /// <returns>每个子图的布局信息列表。</returns>
    public static List<FacetLayout> calculate_layout(
        int facet_count,
        int columns,
        float total_width,
        float total_height,
        float padding)
    {
        if (facet_count <= 0 || columns <= 0) return [];

        var rows = (int)Math.Ceiling((double)facet_count / columns);
        var cell_width = (total_width - (columns + 1) * padding) / columns;
        var cell_height = (total_height - (rows + 1) * padding) / rows;

        if (cell_width < 0) cell_width = 0;

        if (cell_height < 0) cell_height = 0;

        var result = new List<FacetLayout>();
        for (var i = 0; i < facet_count; i++)
        {
            var col = i % columns;
            var row = i / columns;
            var x = padding + col * (cell_width + padding);
            var y = padding + row * (cell_height + padding);
            result.Add(new FacetLayout(x, y, cell_width, cell_height));
        }

        return result;
    }

    #endregion
}
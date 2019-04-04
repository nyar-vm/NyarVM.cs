using System;
using System.Collections.Generic;
using System.Linq;

namespace Plotter.Grammar;

/// <summary>
///     数据变换处理器，执行统计计算、数据聚合、分箱、平滑等操作。
/// </summary>
public static class DataTransformProcessor
{
    #region 公开方法

    /// <summary>
    ///     对数据集执行指定的变换操作。
    /// </summary>
    /// <param name="data">原始数据记录列表，每条记录为字段名到值的映射。</param>
    /// <param name="transform_type">变换类型。</param>
    /// <param name="field">目标字段名。</param>
    /// <param name="group_by_field">分组字段名，可为 null。</param>
    /// <returns>变换后的数据记录列表。</returns>
    public static List<Dictionary<string, object>> apply(
        List<Dictionary<string, object>> data,
        TransformType transform_type,
        string field,
        string? group_by_field = null)
    {
        if (data == null || data.Count == 0) return [];

        switch (transform_type)
        {
            case TransformType.AggregateSum:
                return aggregate_sum(data, field, group_by_field);
            case TransformType.AggregateAverage:
                return aggregate_average(data, field, group_by_field);
            case TransformType.GroupCount:
                return group_count(data, field, group_by_field);
            case TransformType.BinHistogram:
                return bin_histogram(data, field, 10);
            case TransformType.SmoothCurve:
                return smooth_curve(data, field);
            case TransformType.DensityEstimate:
                return density_estimate(data, field, 20);
            default:
                return data;
        }
    }

    #endregion

    #region 聚合求和

    /// <summary>
    ///     按分组字段对目标字段求和聚合。
    /// </summary>
    /// <param name="data">原始数据记录列表。</param>
    /// <param name="field">目标字段名。</param>
    /// <param name="group_by_field">分组字段名，可为 null。</param>
    /// <returns>聚合后的数据记录列表。</returns>
    private static List<Dictionary<string, object>> aggregate_sum(
        List<Dictionary<string, object>> data,
        string field,
        string? group_by_field)
    {
        if (group_by_field == null)
        {
            var sum = data.Select(r => to_double(r, field)).Sum();
            return
            [
                new Dictionary<string, object>
                {
                    [field] = sum
                }
            ];
        }

        return
        [
            .. data
                .GroupBy(r => r.TryGetValue(group_by_field, out var v) ? v?.ToString() ?? "" : "")
                .Select(g => new Dictionary<string, object>
                {
                    [group_by_field] = g.Key,
                    [field] = g.Select(r => to_double(r, field)).Sum()
                })
        ];
    }

    #endregion

    #region 聚合均值

    /// <summary>
    ///     按分组字段对目标字段求均值聚合。
    /// </summary>
    /// <param name="data">原始数据记录列表。</param>
    /// <param name="field">目标字段名。</param>
    /// <param name="group_by_field">分组字段名，可为 null。</param>
    /// <returns>聚合后的数据记录列表。</returns>
    private static List<Dictionary<string, object>> aggregate_average(
        List<Dictionary<string, object>> data,
        string field,
        string? group_by_field)
    {
        if (group_by_field == null)
        {
            var avg = data.Select(r => to_double(r, field)).Average();
            return
            [
                new Dictionary<string, object>
                {
                    [field] = avg
                }
            ];
        }

        return
        [
            .. data
                .GroupBy(r => r.TryGetValue(group_by_field, out var v) ? v?.ToString() ?? "" : "")
                .Select(g => new Dictionary<string, object>
                {
                    [group_by_field] = g.Key,
                    [field] = g.Select(r => to_double(r, field)).Average()
                })
        ];
    }

    #endregion

    #region 分组计数

    /// <summary>
    ///     按分组字段统计每组的记录数。
    /// </summary>
    /// <param name="data">原始数据记录列表。</param>
    /// <param name="field">目标字段名。</param>
    /// <param name="group_by_field">分组字段名，可为 null。</param>
    /// <returns>分组计数结果列表。</returns>
    private static List<Dictionary<string, object>> group_count(
        List<Dictionary<string, object>> data,
        string field,
        string? group_by_field)
    {
        var effective_field = group_by_field ?? field;

        return
        [
            .. data
                .GroupBy(r => r.TryGetValue(effective_field, out var v) ? v?.ToString() ?? "" : "")
                .Select(g => new Dictionary<string, object>
                {
                    [effective_field] = g.Key,
                    ["count"] = (double)g.Count()
                })
        ];
    }

    #endregion

    #region 直方图分箱

    /// <summary>
    ///     将目标字段的值域等分为指定数量的箱，统计每个箱内的记录数。
    /// </summary>
    /// <param name="data">原始数据记录列表。</param>
    /// <param name="field">目标字段名。</param>
    /// <param name="bin_count">箱数量。</param>
    /// <returns>分箱统计结果列表，每条记录包含 bin_start、bin_end、count 字段。</returns>
    private static List<Dictionary<string, object>> bin_histogram(
        List<Dictionary<string, object>> data,
        string field,
        int bin_count)
    {
        var values = data.Select(r => to_double(r, field)).ToList();
        var min_val = values.Min();
        var max_val = values.Max();
        var range = max_val - min_val;

        if (Math.Abs(range) < double.Epsilon)
            return
            [
                new Dictionary<string, object>
                {
                    ["bin_start"] = min_val,
                    ["bin_end"] = max_val,
                    ["count"] = (double)values.Count
                }
            ];

        var bin_width = range / bin_count;
        var bins = new int[bin_count];

        foreach (var v in values)
        {
            var idx = (int)Math.Floor((v - min_val) / bin_width);
            if (idx >= bin_count) idx = bin_count - 1;

            if (idx < 0) idx = 0;

            bins[idx]++;
        }

        var result = new List<Dictionary<string, object>>();
        for (var i = 0; i < bin_count; i++)
            result.Add(new Dictionary<string, object>
            {
                ["bin_start"] = min_val + i * bin_width,
                ["bin_end"] = min_val + (i + 1) * bin_width,
                ["count"] = (double)bins[i]
            });

        return result;
    }

    #endregion

    #region 平滑曲线

    /// <summary>
    ///     对目标字段执行简单移动平均平滑，窗口大小为 3。
    /// </summary>
    /// <param name="data">原始数据记录列表。</param>
    /// <param name="field">目标字段名。</param>
    /// <returns>平滑后的数据记录列表。</returns>
    private static List<Dictionary<string, object>> smooth_curve(
        List<Dictionary<string, object>> data,
        string field)
    {
        const int window = 3;
        var half = window / 2;
        var values = data.Select(r => to_double(r, field)).ToList();
        var result = new List<Dictionary<string, object>>();

        for (var i = 0; i < data.Count; i++)
        {
            var record = new Dictionary<string, object>(data[i]);
            var start = Math.Max(0, i - half);
            var end = Math.Min(data.Count - 1, i + half);
            var count = end - start + 1;
            var sum = 0.0;
            for (var j = start; j <= end; j++) sum += values[j];

            record[field] = sum / count;
            result.Add(record);
        }

        return result;
    }

    #endregion

    #region 密度估计

    /// <summary>
    ///     对目标字段执行简单核密度估计，使用高斯核。
    /// </summary>
    /// <param name="data">原始数据记录列表。</param>
    /// <param name="field">目标字段名。</param>
    /// <param name="point_count">估计点数量。</param>
    /// <returns>密度估计结果列表，每条记录包含 x 和 density 字段。</returns>
    private static List<Dictionary<string, object>> density_estimate(
        List<Dictionary<string, object>> data,
        string field,
        int point_count)
    {
        var values = data.Select(r => to_double(r, field)).ToList();
        var n = values.Count;
        if (n == 0) return [];

        var mean = values.Average();
        var variance = values.Select(v => (v - mean) * (v - mean)).Average();
        var std_dev = Math.Sqrt(variance);

        if (std_dev < double.Epsilon) std_dev = 1.0;

        var bandwidth = 1.06 * std_dev * Math.Pow(n, -0.2);
        var min_val = values.Min() - 3 * bandwidth;
        var max_val = values.Max() + 3 * bandwidth;
        var step = (max_val - min_val) / (point_count - 1);

        var result = new List<Dictionary<string, object>>();
        for (var i = 0; i < point_count; i++)
        {
            var x = min_val + i * step;
            var density = 0.0;
            for (var j = 0; j < n; j++)
            {
                var u = (x - values[j]) / bandwidth;
                density += Math.Exp(-0.5 * u * u);
            }

            density /= n * bandwidth * Math.Sqrt(2 * Math.PI);
            result.Add(new Dictionary<string, object>
            {
                ["x"] = x,
                ["density"] = density
            });
        }

        return result;
    }

    #endregion

    #region 辅助方法

    /// <summary>
    ///     将记录中指定字段的值转换为双精度浮点数。
    /// </summary>
    /// <param name="record">数据记录。</param>
    /// <param name="field">字段名。</param>
    /// <returns>转换后的双精度浮点数值。</returns>
    private static double to_double(Dictionary<string, object> record, string field)
    {
        if (!record.TryGetValue(field, out var value) || value == null) return 0.0;

        return Convert.ToDouble(value);
    }

    #endregion
}
using System;

namespace Plotter.Grammar;

/// <summary>
///     优美刻度计算器，使用 Nice Numbers 算法计算坐标轴刻度。
///     确保刻度值为人类易读的整数或简单小数。
/// </summary>
internal static class NiceScale
{
    /// <summary>
    ///     根据数据范围和最大刻度数计算优美的刻度区间。
    /// </summary>
    /// <param name="data_min">数据最小值。</param>
    /// <param name="data_max">数据最大值。</param>
    /// <param name="max_ticks">最大刻度数量，默认为 10。</param>
    /// <returns>包含最小值、最大值、步长和刻度数组的元组。</returns>
    public static (double min, double max, double step, double[] ticks) calculate(
        double data_min, double data_max, int max_ticks = 10)
    {
        if (Math.Abs(data_max - data_min) < double.Epsilon)
        {
            data_min = data_min == 0 ? -1 : data_min - Math.Abs(data_min) * 0.1;
            data_max = data_max == 0 ? 1 : data_max + Math.Abs(data_max) * 0.1;
        }

        var range = nice_num(data_max - data_min, false);
        var step = nice_num(range / (max_ticks - 1), true);
        var nice_min = Math.Floor(data_min / step) * step;
        var nice_max = Math.Ceiling(data_max / step) * step;

        var tick_count = (int)Math.Round((nice_max - nice_min) / step) + 1;
        var ticks = new double[tick_count];

        for (var i = 0; i < tick_count; i++) ticks[i] = nice_min + i * step;

        return (nice_min, nice_max, step, ticks);
    }

    /// <summary>
    ///     计算一个"优美"的近似数值。
    ///     当 round 为 true 时向下取整到最近的优美数，否则取最近的优美数。
    /// </summary>
    /// <param name="value">输入数值。</param>
    /// <param name="round">是否向下取整。</param>
    /// <returns>优美的近似数值。</returns>
    private static double nice_num(double value, bool round)
    {
        var exponent = Math.Floor(Math.Log10(Math.Abs(value)));
        var fraction = value / Math.Pow(10.0, exponent);

        double nice_fraction;

        if (round)
        {
            if (fraction < 1.5)
                nice_fraction = 1;
            else if (fraction < 3)
                nice_fraction = 2;
            else if (fraction < 7)
                nice_fraction = 5;
            else
                nice_fraction = 10;
        }
        else
        {
            if (fraction <= 1)
                nice_fraction = 1;
            else if (fraction <= 2)
                nice_fraction = 2;
            else if (fraction <= 5)
                nice_fraction = 5;
            else
                nice_fraction = 10;
        }

        return nice_fraction * Math.Pow(10.0, exponent);
    }
}
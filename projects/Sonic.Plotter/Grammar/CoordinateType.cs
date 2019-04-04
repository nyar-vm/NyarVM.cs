namespace Plotter.Grammar;

/// <summary>
///     坐标系类型枚举，定义图表的坐标系方式。
/// </summary>
public enum CoordinateType
{
    /// <summary>
    ///     笛卡尔坐标系。
    /// </summary>
    Cartesian,

    /// <summary>
    ///     极坐标系。
    /// </summary>
    Polar,

    /// <summary>
    ///     反转坐标系。
    /// </summary>
    Reversed
}
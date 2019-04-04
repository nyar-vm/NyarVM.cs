using Plotter.Grammar;

namespace Plotter.Schema;

/// <summary>
///     坐标系配置，定义图表的坐标系类型。
/// </summary>
public class CoordinateSystemConfig
{
    /// <summary>
    ///     坐标系类型。
    /// </summary>
    public CoordinateType coordinate_type { get; set; }
}
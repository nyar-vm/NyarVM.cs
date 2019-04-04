using Plotter.Grammar;

namespace Plotter.Schema;

/// <summary>
///     数据变换配置，定义数据聚合与变换方式。
/// </summary>
public class DataTransformConfig
{
    /// <summary>
    ///     变换类型。
    /// </summary>
    public TransformType transform_type { get; set; }

    /// <summary>
    ///     目标字段名称。
    /// </summary>
    public string field { get; set; } = "";

    /// <summary>
    ///     分组字段名称。
    /// </summary>
    public string? group_by_field { get; set; }
}
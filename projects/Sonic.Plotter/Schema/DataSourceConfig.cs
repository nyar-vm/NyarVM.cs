using System.Collections.Generic;

namespace Plotter.Schema;

/// <summary>
///     数据源配置，定义图表数据的来源和格式。
/// </summary>
public class DataSourceConfig
{
    /// <summary>
    ///     数据源类型标识。
    /// </summary>
    public string type { get; set; } = "";

    /// <summary>
    ///     数据表名称。
    /// </summary>
    public string table_name { get; set; } = "";

    /// <summary>
    ///     内嵌数据行列表，每行为字段名到值的映射。
    /// </summary>
    public List<Dictionary<string, object>>? data { get; set; }
}
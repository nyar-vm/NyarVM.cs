using System.Collections.Generic;

namespace Plotter.Grammar;

/// <summary>
///     分面分组，包含字段值和对应的数据子集。
/// </summary>
public sealed class FacetGroup
{
    /// <summary>
    ///     初始化 <see cref="FacetGroup" /> 类的新实例。
    /// </summary>
    /// <param name="field_value">分组字段值。</param>
    /// <param name="data">该分组的数据子集。</param>
    public FacetGroup(string field_value, List<Dictionary<string, object>> data)
    {
        this.field_value = field_value;
        this.data = data;
    }

    /// <summary>
    ///     分组字段值。
    /// </summary>
    public string field_value { get; }

    /// <summary>
    ///     该分组对应的数据子集。
    /// </summary>
    public List<Dictionary<string, object>> data { get; }
}
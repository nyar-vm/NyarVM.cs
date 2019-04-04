using System.Collections.Generic;
using Plotter.Interaction;

namespace Plotter.Schema;

/// <summary>
///     交互配置，定义图表支持的交互模式列表。
/// </summary>
public class InteractionConfig
{
    /// <summary>
    ///     交互模式列表。
    /// </summary>
    public List<InteractionMode> modes { get; set; } = [];
}
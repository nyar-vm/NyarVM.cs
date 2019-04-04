using System.Collections.Generic;

namespace Plotter.Schema;

#region 图表配置数据模型

/// <summary>
///     图表配置的完整数据模型，用于 JSON 序列化与反序列化。
/// </summary>
public class PlotterSchema
{
    /// <summary>
    ///     配置版本号。
    /// </summary>
    public string version { get; set; } = "1.0";

    /// <summary>
    ///     标题配置。
    /// </summary>
    public TitleConfig? title { get; set; }

    /// <summary>
    ///     画布尺寸配置。
    /// </summary>
    public SizeConfig? size { get; set; }

    /// <summary>
    ///     边距配置。
    /// </summary>
    public MarginConfig? margin { get; set; }

    /// <summary>
    ///     数据源配置。
    /// </summary>
    public DataSourceConfig? data_source { get; set; }

    /// <summary>
    ///     视觉映射配置。
    /// </summary>
    public VisualMappingConfig? visual_mapping { get; set; }

    /// <summary>
    ///     数据变换配置列表。
    /// </summary>
    public List<DataTransformConfig>? data_transforms { get; set; }

    /// <summary>
    ///     图形图层配置列表。
    /// </summary>
    public List<ShapeLayerConfig>? shape_layers { get; set; }

    /// <summary>
    ///     刻度配置。
    /// </summary>
    public ValueScaleConfig? value_scale { get; set; }

    /// <summary>
    ///     坐标系配置。
    /// </summary>
    public CoordinateSystemConfig? coordinate_system { get; set; }

    /// <summary>
    ///     布局面板配置。
    /// </summary>
    public LayoutPanelConfig? layout_panel { get; set; }

    /// <summary>
    ///     交互配置。
    /// </summary>
    public InteractionConfig? interaction { get; set; }

    /// <summary>
    ///     动画配置。
    /// </summary>
    public AnimationConfig? animation { get; set; }

    /// <summary>
    ///     主题配置。
    /// </summary>
    public ThemeConfig? theme { get; set; }

    /// <summary>
    ///     标注配置列表。
    /// </summary>
    public List<AnnotationConfig>? annotations { get; set; }
}

#endregion
using System.Collections.Generic;
using Plotter.Animation;
using Plotter.Extensions;
using Plotter.Grammar;
using Plotter.Interaction;
using Plotter.Rendering;

namespace Plotter.Core;

/// <summary>
///     图表总入口，对外唯一门面。
///     实现图形语法的链式 API，支持多端渲染和格式导出。
/// </summary>
public class ChartCanvas : IChart
{
    #region IChart 实现

    /// <summary>
    ///     在指定画布上渲染图表，实现 <see cref="IChart" /> 接口。
    /// </summary>
    /// <param name="canvas">目标画布。</param>
    void IChart.render(ICanvas canvas)
    {
        var svg_canvas = new SvgRenderCanvas(canvas.width, canvas.height);
        render_to_canvas(svg_canvas);
        canvas.clear();
    }

    #endregion

    #region 字段

    /// <summary>
    ///     画布宽度（像素）。
    /// </summary>
    private int _width = 800;

    /// <summary>
    ///     画布高度（像素）。
    /// </summary>
    private int _height = 600;

    /// <summary>
    ///     主标题文本。
    /// </summary>
    private string _main_title = "";

    /// <summary>
    ///     副标题文本。
    /// </summary>
    private string _sub_title = "";

    /// <summary>
    ///     上边距（像素）。
    /// </summary>
    private int _margin_top = 20;

    /// <summary>
    ///     下边距（像素）。
    /// </summary>
    private int _margin_bottom = 30;

    /// <summary>
    ///     左边距（像素）。
    /// </summary>
    private int _margin_left = 40;

    /// <summary>
    ///     右边距（像素）。
    /// </summary>
    private int _margin_right = 20;

    /// <summary>
    ///     图层管理器。
    /// </summary>
    private readonly LayerManager _layer_manager = new();

    /// <summary>
    ///     图形图层构建器列表。
    /// </summary>
    private readonly List<ShapeLayerBuilder> _shape_layer_builders = new();

    /// <summary>
    ///     标注构建器列表。
    /// </summary>
    private readonly List<AnnotationBuilder> _annotation_builders = new();

    /// <summary>
    ///     视觉映射构建器实例。
    /// </summary>
    private VisualMappingBuilder? _visual_mapping_builder;

    /// <summary>
    ///     数据变换构建器实例。
    /// </summary>
    private DataTransformBuilder? _data_transform_builder;

    /// <summary>
    ///     刻度构建器实例。
    /// </summary>
    private ValueScaleBuilder? _value_scale_builder;

    /// <summary>
    ///     坐标系构建器实例。
    /// </summary>
    private CoordinateSystemBuilder? _coordinate_system_builder;

    /// <summary>
    ///     布局面板构建器实例。
    /// </summary>
    private LayoutPanelBuilder? _layout_panel_builder;

    /// <summary>
    ///     交互构建器实例。
    /// </summary>
    private InteractionBuilder? _interaction_builder;

    /// <summary>
    ///     动画构建器实例。
    /// </summary>
    private AnimationBuilder? _animation_builder;

    /// <summary>
    ///     主题构建器实例。
    /// </summary>
    private ThemeBuilder? _theme_builder;

    #endregion

    #region 配置方法

    /// <summary>
    ///     设置画布尺寸。
    /// </summary>
    /// <param name="width">画布宽度（像素）。</param>
    /// <param name="height">画布高度（像素）。</param>
    /// <returns>当前图表画布实例，支持链式调用。</returns>
    public ChartCanvas with_size(int width, int height)
    {
        _width = width;
        _height = height;
        return this;
    }

    /// <summary>
    ///     设置图表标题。
    /// </summary>
    /// <param name="main">主标题。</param>
    /// <param name="sub">副标题，默认为空。</param>
    /// <returns>当前图表画布实例，支持链式调用。</returns>
    public ChartCanvas with_title(string main, string sub = "")
    {
        _main_title = main;
        _sub_title = sub;
        return this;
    }

    /// <summary>
    ///     设置图表边距。
    /// </summary>
    /// <param name="top">上边距（像素）。</param>
    /// <param name="bottom">下边距（像素）。</param>
    /// <param name="left">左边距（像素）。</param>
    /// <param name="right">右边距（像素）。</param>
    /// <returns>当前图表画布实例，支持链式调用。</returns>
    public ChartCanvas with_margin(int top, int bottom, int left, int right)
    {
        _margin_top = top;
        _margin_bottom = bottom;
        _margin_left = left;
        _margin_right = right;
        return this;
    }

    #endregion

    #region 构建器入口方法

    /// <summary>
    ///     获取视觉映射构建器，用于配置数据字段到视觉通道的映射。
    /// </summary>
    /// <returns>视觉映射构建器实例。</returns>
    public VisualMappingBuilder use_visual_mapping()
    {
        return _visual_mapping_builder ??= new VisualMappingBuilder(this);
    }

    /// <summary>
    ///     获取数据变换构建器，用于配置数据聚合与变换。
    /// </summary>
    /// <returns>数据变换构建器实例。</returns>
    public DataTransformBuilder use_data_transform()
    {
        return _data_transform_builder ??= new DataTransformBuilder(this);
    }

    /// <summary>
    ///     创建图形图层构建器，用于添加新的图形图层。
    /// </summary>
    /// <returns>图形图层构建器实例。</returns>
    public ShapeLayerBuilder add_shape_layer()
    {
        var builder = new ShapeLayerBuilder(this);
        _shape_layer_builders.Add(builder);
        return builder;
    }

    /// <summary>
    ///     获取刻度构建器，用于配置坐标轴刻度。
    /// </summary>
    /// <returns>刻度构建器实例。</returns>
    public ValueScaleBuilder use_value_scale()
    {
        return _value_scale_builder ??= new ValueScaleBuilder(this);
    }

    /// <summary>
    ///     获取坐标系构建器，用于配置坐标系类型。
    /// </summary>
    /// <returns>坐标系构建器实例。</returns>
    public CoordinateSystemBuilder use_coordinate_system()
    {
        return _coordinate_system_builder ??= new CoordinateSystemBuilder(this);
    }

    /// <summary>
    ///     获取布局面板构建器，用于配置多图布局。
    /// </summary>
    /// <returns>布局面板构建器实例。</returns>
    public LayoutPanelBuilder use_layout_panel()
    {
        return _layout_panel_builder ??= new LayoutPanelBuilder(this);
    }

    /// <summary>
    ///     获取交互构建器，用于配置图表交互行为。
    /// </summary>
    /// <returns>交互构建器实例。</returns>
    public InteractionBuilder use_interaction()
    {
        return _interaction_builder ??= new InteractionBuilder(this);
    }

    /// <summary>
    ///     获取动画构建器，用于配置图表动画效果。
    /// </summary>
    /// <returns>动画构建器实例。</returns>
    public AnimationBuilder use_animation()
    {
        return _animation_builder ??= new AnimationBuilder(this);
    }

    /// <summary>
    ///     获取主题构建器，用于配置图表主题风格。
    /// </summary>
    /// <returns>主题构建器实例。</returns>
    public ThemeBuilder use_theme()
    {
        return _theme_builder ??= new ThemeBuilder(this);
    }

    /// <summary>
    ///     创建标注构建器，用于添加图表标注。
    /// </summary>
    /// <returns>标注构建器实例。</returns>
    public AnnotationBuilder add_annotation()
    {
        var builder = new AnnotationBuilder(this);
        _annotation_builders.Add(builder);
        return builder;
    }

    #endregion

    #region 导出方法

    /// <summary>
    ///     将图表导出为 SVG 格式字符串。
    /// </summary>
    /// <returns>SVG 文档字符串。</returns>
    public string export_to_svg()
    {
        var canvas = new SvgRenderCanvas(_width, _height);
        render_to_canvas(canvas);
        return canvas.to_svg_string();
    }

    /// <summary>
    ///     将图表导出为 PNG 格式字节数组。
    /// </summary>
    /// <returns>PNG 图像字节数组。</returns>
    public byte[] export_to_png()
    {
        using var canvas = new BitmapRenderCanvas(_width, _height);
        render_to_canvas(canvas);
        return canvas.to_png_bytes();
    }

    #endregion

    #region 内部访问器

    /// <summary>
    ///     获取画布宽度。
    /// </summary>
    internal int width => _width;

    /// <summary>
    ///     获取画布高度。
    /// </summary>
    internal int height => _height;

    /// <summary>
    ///     获取主标题文本。
    /// </summary>
    internal string main_title => _main_title;

    /// <summary>
    ///     获取副标题文本。
    /// </summary>
    internal string sub_title => _sub_title;

    /// <summary>
    ///     获取上边距。
    /// </summary>
    internal int margin_top => _margin_top;

    /// <summary>
    ///     获取下边距。
    /// </summary>
    internal int margin_bottom => _margin_bottom;

    /// <summary>
    ///     获取左边距。
    /// </summary>
    internal int margin_left => _margin_left;

    /// <summary>
    ///     获取右边距。
    /// </summary>
    internal int margin_right => _margin_right;

    /// <summary>
    ///     获取视觉映射构建器实例。
    /// </summary>
    internal VisualMappingBuilder? visual_mapping_builder => _visual_mapping_builder;

    /// <summary>
    ///     获取数据变换构建器实例。
    /// </summary>
    internal DataTransformBuilder? data_transform_builder => _data_transform_builder;

    /// <summary>
    ///     获取刻度构建器实例。
    /// </summary>
    internal ValueScaleBuilder? value_scale_builder => _value_scale_builder;

    /// <summary>
    ///     获取坐标系构建器实例。
    /// </summary>
    internal CoordinateSystemBuilder? coordinate_system_builder => _coordinate_system_builder;

    /// <summary>
    ///     获取布局面板构建器实例。
    /// </summary>
    internal LayoutPanelBuilder? layout_panel_builder => _layout_panel_builder;

    /// <summary>
    ///     获取交互构建器实例。
    /// </summary>
    internal InteractionBuilder? interaction_builder => _interaction_builder;

    /// <summary>
    ///     获取动画构建器实例。
    /// </summary>
    internal AnimationBuilder? animation_builder => _animation_builder;

    /// <summary>
    ///     获取主题构建器实例。
    /// </summary>
    internal ThemeBuilder? theme_builder => _theme_builder;

    /// <summary>
    ///     获取图形图层构建器只读列表。
    /// </summary>
    internal IReadOnlyList<ShapeLayerBuilder> shape_layer_builders => _shape_layer_builders;

    /// <summary>
    ///     获取标注构建器只读列表。
    /// </summary>
    internal IReadOnlyList<AnnotationBuilder> annotation_builders => _annotation_builders;

    #endregion

    #region 内部方法

    /// <summary>
    ///     添加图层到图层管理器。
    /// </summary>
    /// <param name="layer">要添加的图层。</param>
    internal void add_layer(ChartLayer layer)
    {
        _layer_manager.add_layer(layer);
    }

    /// <summary>
    ///     将视觉映射配置应用到所有已注册的 <see cref="ShapeLayer" /> 图层。
    /// </summary>
    /// <param name="mapping_builder">视觉映射构建器。</param>
    internal void apply_visual_mapping(VisualMappingBuilder mapping_builder)
    {
        foreach (var layer in get_shape_layers()) mapping_builder.apply_to(layer);
    }

    /// <summary>
    ///     获取图层管理器中所有 <see cref="ShapeLayer" /> 类型的图层。
    /// </summary>
    /// <returns>图形图层列表。</returns>
    private List<ShapeLayer> get_shape_layers()
    {
        return _layer_manager.get_layers_of_type<ShapeLayer>();
    }

    /// <summary>
    ///     将所有图层渲染到指定画布上。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    private void render_to_canvas(IRenderCanvas canvas)
    {
        canvas.begin_render();
        _layer_manager.render_all_force(canvas);
        canvas.end_render();
    }

    #endregion
}
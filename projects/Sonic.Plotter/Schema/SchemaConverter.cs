using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Plotter.Core;
using Plotter.Extensions;
using Plotter.Interaction;

namespace Plotter.Schema;

/// <summary>
///     Schema 转换器，提供 JSON 与 ChartCanvas 之间的双向转换。
/// </summary>
public static class SchemaConverter
{
    /// <summary>
    ///     JSON 序列化选项，使用驼峰命名策略和缩进格式。
    /// </summary>
    private static readonly JsonSerializerOptions _json_options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>
    ///     从 JSON 字符串构建 ChartCanvas 实例。
    /// </summary>
    /// <param name="json">JSON 配置字符串。</param>
    /// <returns>根据配置构建的图表画布实例。</returns>
    /// <exception cref="FormatException">当 JSON 无法解析为有效的配置时抛出。</exception>
    public static ChartCanvas from_json(string json)
    {
        var schema = JsonSerializer.Deserialize<PlotterSchema>(json, _json_options);
        if (schema == null) throw new FormatException("无法解析 JSON 配置");
        return build_canvas(schema);
    }

    /// <summary>
    ///     将 ChartCanvas 导出为 JSON 字符串。
    /// </summary>
    /// <param name="chart">要导出的图表画布实例。</param>
    /// <returns>格式化的 JSON 配置字符串。</returns>
    public static string to_json(ChartCanvas chart)
    {
        var schema = extract_schema(chart);
        return JsonSerializer.Serialize(schema, _json_options);
    }

    #region 构建 ChartCanvas

    /// <summary>
    ///     根据 Schema 配置构建 ChartCanvas 实例。
    /// </summary>
    /// <param name="schema">图表配置数据模型。</param>
    /// <returns>配置完成的图表画布实例。</returns>
    private static ChartCanvas build_canvas(PlotterSchema schema)
    {
        var canvas = new ChartCanvas();

        apply_size(canvas, schema.size);
        apply_title(canvas, schema.title);
        apply_margin(canvas, schema.margin);
        apply_visual_mapping(canvas, schema.visual_mapping);
        apply_data_transforms(canvas, schema.data_transforms);
        apply_shape_layers(canvas, schema.shape_layers);
        apply_value_scale(canvas, schema.value_scale);
        apply_coordinate_system(canvas, schema.coordinate_system);
        apply_layout_panel(canvas, schema.layout_panel);
        apply_interaction(canvas, schema.interaction);
        apply_animation(canvas, schema.animation);
        apply_theme(canvas, schema.theme);
        apply_annotations(canvas, schema.annotations);

        return canvas;
    }

    /// <summary>
    ///     应用画布尺寸配置。
    /// </summary>
    private static void apply_size(ChartCanvas canvas, SizeConfig? config)
    {
        if (config == null) return;
        canvas.with_size(config.width, config.height);
    }

    /// <summary>
    ///     应用标题配置。
    /// </summary>
    private static void apply_title(ChartCanvas canvas, TitleConfig? config)
    {
        if (config == null) return;
        canvas.with_title(config.main_text, config.sub_text);
    }

    /// <summary>
    ///     应用边距配置。
    /// </summary>
    private static void apply_margin(ChartCanvas canvas, MarginConfig? config)
    {
        if (config == null) return;
        canvas.with_margin(config.top, config.bottom, config.left, config.right);
    }

    /// <summary>
    ///     应用视觉映射配置。
    /// </summary>
    private static void apply_visual_mapping(ChartCanvas canvas, VisualMappingConfig? config)
    {
        if (config == null) return;
        var builder = canvas.use_visual_mapping();
        if (!string.IsNullOrEmpty(config.x_field)) builder.map_x_field(config.x_field);
        if (!string.IsNullOrEmpty(config.y_field)) builder.map_y_field(config.y_field);
        if (!string.IsNullOrEmpty(config.fill_color_field)) builder.map_fill_color(config.fill_color_field);
        if (!string.IsNullOrEmpty(config.stroke_color_field)) builder.map_stroke_color(config.stroke_color_field);
        if (!string.IsNullOrEmpty(config.size_field)) builder.map_size(config.size_field);
        if (!string.IsNullOrEmpty(config.fixed_fill)) builder.with_fixed_fill(config.fixed_fill);
        if (!string.IsNullOrEmpty(config.fixed_stroke)) builder.with_fixed_stroke(config.fixed_stroke);
        if (config.fixed_size != 0) builder.with_fixed_size(config.fixed_size);
        builder.build();
    }

    /// <summary>
    ///     应用数据变换配置列表。
    /// </summary>
    private static void apply_data_transforms(ChartCanvas canvas, List<DataTransformConfig>? configs)
    {
        if (configs == null || configs.Count == 0) return;
        var builder = canvas.use_data_transform();
        var first = configs[0];
        builder.with_transform_type(first.transform_type);
        if (!string.IsNullOrEmpty(first.group_by_field)) builder.group_by(first.group_by_field);
        builder.build();
    }

    /// <summary>
    ///     应用图形图层配置列表。
    /// </summary>
    private static void apply_shape_layers(ChartCanvas canvas, List<ShapeLayerConfig>? configs)
    {
        if (configs == null) return;
        foreach (var config in configs)
        {
            var builder = canvas.add_shape_layer();
            builder.with_shape_type(config.shape_type);
            if (Math.Abs(config.line_width - 1.0f) > 0.001f) builder.with_line_width(config.line_width);
            if (Math.Abs(config.layer_opacity - 1.0) > 0.001) builder.with_layer_opacity(config.layer_opacity);
            builder.build();
        }
    }

    /// <summary>
    ///     应用刻度配置。
    /// </summary>
    private static void apply_value_scale(ChartCanvas canvas, ValueScaleConfig? config)
    {
        if (config == null) return;
        var builder = canvas.use_value_scale();
        builder.with_x_scale_type(config.x_scale_type);
        builder.with_y_scale_type(config.y_scale_type);
        if (Math.Abs(config.x_min) > 0.001 || Math.Abs(config.x_max) > 0.001)
            builder.with_range(config.x_min, config.x_max);
        if (config.tick_count != 5) builder.with_tick_count(config.tick_count);
        builder.build();
    }

    /// <summary>
    ///     应用坐标系配置。
    /// </summary>
    private static void apply_coordinate_system(ChartCanvas canvas, CoordinateSystemConfig? config)
    {
        if (config == null) return;
        var builder = canvas.use_coordinate_system();
        builder.with_coordinate_type(config.coordinate_type);
        builder.build();
    }

    /// <summary>
    ///     应用布局面板配置。
    /// </summary>
    private static void apply_layout_panel(ChartCanvas canvas, LayoutPanelConfig? config)
    {
        if (config == null) return;
        var builder = canvas.use_layout_panel();
        if (config.row_count > 0) builder.split_by_row(config.row_count);
        if (config.column_count > 0) builder.with_column_count(config.column_count);
        builder.build();
    }

    /// <summary>
    ///     应用交互配置。
    /// </summary>
    private static void apply_interaction(ChartCanvas canvas, InteractionConfig? config)
    {
        if (config == null || config.modes == null || config.modes.Count == 0) return;
        var builder = canvas.use_interaction();
        builder.with_mode(config.modes[0]);
        builder.build();
    }

    /// <summary>
    ///     应用动画配置。
    /// </summary>
    private static void apply_animation(ChartCanvas canvas, AnimationConfig? config)
    {
        if (config == null) return;
        var builder = canvas.use_animation();
        builder.with_enabled(config.enabled);
        builder.with_duration(config.duration);
        builder.with_easing_style(config.easing_style);
        if (config.enter_animation) builder.with_enter_animation("fadeIn");
        if (config.update_animation) builder.with_update_animation("morph");
        if (config.exit_animation) builder.with_exit_animation("fadeOut");
        builder.build();
    }

    /// <summary>
    ///     应用主题配置。
    /// </summary>
    private static void apply_theme(ChartCanvas canvas, ThemeConfig? config)
    {
        if (config == null) return;
        var builder = canvas.use_theme();
        builder.with_preset(config.preset);
        if (config.custom_colors != null && config.custom_colors.Count > 0)
            builder.with_custom_colors(config.custom_colors.ToArray());
        if (!string.IsNullOrEmpty(config.font_family)) builder.with_font_family(config.font_family);
        if (!string.IsNullOrEmpty(config.background_color)) builder.with_background_color(config.background_color);
        builder.build();
    }

    /// <summary>
    ///     应用标注配置列表。
    /// </summary>
    private static void apply_annotations(ChartCanvas canvas, List<AnnotationConfig>? configs)
    {
        if (configs == null) return;
        foreach (var config in configs)
        {
            var builder = canvas.add_annotation();
            if (!string.IsNullOrEmpty(config.text)) builder.with_text(config.text);
            builder.build();
        }
    }

    #endregion

    #region 提取 Schema

    /// <summary>
    ///     从 ChartCanvas 实例提取配置，构建 PlotterSchema 数据模型。
    /// </summary>
    /// <param name="chart">源图表画布实例。</param>
    /// <returns>提取的图表配置数据模型。</returns>
    private static PlotterSchema extract_schema(ChartCanvas chart)
    {
        var schema = new PlotterSchema();

        extract_size(chart, schema);
        extract_title(chart, schema);
        extract_margin(chart, schema);
        extract_visual_mapping(chart, schema);
        extract_data_transform(chart, schema);
        extract_shape_layers(chart, schema);
        extract_value_scale(chart, schema);
        extract_coordinate_system(chart, schema);
        extract_layout_panel(chart, schema);
        extract_interaction(chart, schema);
        extract_animation(chart, schema);
        extract_theme(chart, schema);
        extract_annotations(chart, schema);

        return schema;
    }

    /// <summary>
    ///     提取画布尺寸配置。
    /// </summary>
    private static void extract_size(ChartCanvas chart, PlotterSchema schema)
    {
        schema.size = new SizeConfig
        {
            width = chart.width,
            height = chart.height
        };
    }

    /// <summary>
    ///     提取标题配置。
    /// </summary>
    private static void extract_title(ChartCanvas chart, PlotterSchema schema)
    {
        schema.title = new TitleConfig
        {
            main_text = chart.main_title,
            sub_text = chart.sub_title
        };
    }

    /// <summary>
    ///     提取边距配置。
    /// </summary>
    private static void extract_margin(ChartCanvas chart, PlotterSchema schema)
    {
        schema.margin = new MarginConfig
        {
            top = chart.margin_top,
            bottom = chart.margin_bottom,
            left = chart.margin_left,
            right = chart.margin_right
        };
    }

    /// <summary>
    ///     提取视觉映射配置。
    /// </summary>
    private static void extract_visual_mapping(ChartCanvas chart, PlotterSchema schema)
    {
        var builder = chart.visual_mapping_builder;
        if (builder == null) return;
        schema.visual_mapping = new VisualMappingConfig
        {
            x_field = builder.x_field,
            y_field = builder.y_field,
            fill_color_field = builder.fill_color_field,
            stroke_color_field = builder.stroke_color_field,
            size_field = builder.size_field,
            fixed_fill = builder.fixed_fill?.to_svg_string() ?? "",
            fixed_stroke = builder.fixed_stroke?.to_svg_string() ?? "",
            fixed_size = builder.fixed_size ?? 0.0
        };
    }

    /// <summary>
    ///     提取数据变换配置。
    /// </summary>
    private static void extract_data_transform(ChartCanvas chart, PlotterSchema schema)
    {
        var builder = chart.data_transform_builder;
        if (builder == null) return;
        schema.data_transforms =
        [

            new()
            {
                transform_type = builder._transform_type,
                group_by_field = string.IsNullOrEmpty(builder._group_by_field) ? null : builder._group_by_field
            }
        ];
    }

    /// <summary>
    ///     提取图形图层配置列表。
    /// </summary>
    private static void extract_shape_layers(ChartCanvas chart, PlotterSchema schema)
    {
        var builders = chart.shape_layer_builders;
        if (builders.Count == 0) return;
        schema.shape_layers = new List<ShapeLayerConfig>(builders.Count);
        foreach (var builder in builders)
            schema.shape_layers.Add(new ShapeLayerConfig
            {
                shape_type = builder.shape_type,
                line_width = builder.line_width,
                layer_opacity = builder.layer_opacity
            });
    }

    /// <summary>
    ///     提取刻度配置。
    /// </summary>
    private static void extract_value_scale(ChartCanvas chart, PlotterSchema schema)
    {
        var builder = chart.value_scale_builder;
        if (builder == null) return;
        schema.value_scale = new ValueScaleConfig
        {
            x_scale_type = builder._x_scale_type,
            y_scale_type = builder._y_scale_type,
            x_min = builder._range_min,
            x_max = builder._range_max,
            tick_count = builder._tick_count
        };
    }

    /// <summary>
    ///     提取坐标系配置。
    /// </summary>
    private static void extract_coordinate_system(ChartCanvas chart, PlotterSchema schema)
    {
        var builder = chart.coordinate_system_builder;
        if (builder == null) return;
        schema.coordinate_system = new CoordinateSystemConfig
        {
            coordinate_type = builder._coordinate_type
        };
    }

    /// <summary>
    ///     提取布局面板配置。
    /// </summary>
    private static void extract_layout_panel(ChartCanvas chart, PlotterSchema schema)
    {
        var builder = chart.layout_panel_builder;
        if (builder == null) return;
        schema.layout_panel = new LayoutPanelConfig
        {
            row_count = builder._row_count,
            column_count = builder._column_count
        };
    }

    /// <summary>
    ///     提取交互配置。
    /// </summary>
    private static void extract_interaction(ChartCanvas chart, PlotterSchema schema)
    {
        var builder = chart.interaction_builder;
        if (builder == null) return;
        schema.interaction = new InteractionConfig
        {
            modes = [builder._mode]
        };
    }

    /// <summary>
    ///     提取动画配置。
    /// </summary>
    private static void extract_animation(ChartCanvas chart, PlotterSchema schema)
    {
        var builder = chart.animation_builder;
        if (builder == null) return;
        schema.animation = new AnimationConfig
        {
            enabled = builder._enabled,
            duration = builder._duration,
            easing_style = builder._easing_style,
            enter_animation = !string.IsNullOrEmpty(builder._enter_animation),
            update_animation = !string.IsNullOrEmpty(builder._update_animation),
            exit_animation = !string.IsNullOrEmpty(builder._exit_animation)
        };
    }

    /// <summary>
    ///     提取主题配置。
    /// </summary>
    private static void extract_theme(ChartCanvas chart, PlotterSchema schema)
    {
        var builder = chart.theme_builder;
        if (builder == null) return;
        schema.theme = new ThemeConfig
        {
            preset = builder._preset,
            custom_colors = builder._custom_colors.Length > 0
                ? new List<string>(builder._custom_colors)
                : null,
            font_family = builder._font_family,
            background_color = builder._background_color
        };
    }

    /// <summary>
    ///     提取标注配置列表。
    /// </summary>
    private static void extract_annotations(ChartCanvas chart, PlotterSchema schema)
    {
        var builders = chart.annotation_builders;
        if (builders.Count == 0) return;
        schema.annotations = new List<AnnotationConfig>(builders.Count);
        foreach (var builder in builders)
        {
            var annotation_type = DetermineAnnotationType(builder);
            schema.annotations.Add(new AnnotationConfig
            {
                annotation_type = annotation_type,
                text = string.IsNullOrEmpty(builder._text) ? null : builder._text
            });
        }
    }

    /// <summary>
    ///     根据标注构建器的配置推断标注类型。
    /// </summary>
    private static string DetermineAnnotationType(AnnotationBuilder builder)
    {
        if (!string.IsNullOrEmpty(builder._text)) return "text";
        if (!string.IsNullOrEmpty(builder._line)) return "line";
        if (!string.IsNullOrEmpty(builder._region)) return "region";
        return "";
    }

    #endregion
}
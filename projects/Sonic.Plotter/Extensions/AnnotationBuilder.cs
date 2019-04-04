using Plotter.Core;

namespace Plotter.Extensions;

/// <summary>
///     标注构建器，用于配置图表的文本、线条和区域标注。
/// </summary>
public sealed class AnnotationBuilder
{
    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     初始化 <see cref="AnnotationBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public AnnotationBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    ///     文本标注内容。
    /// </summary>
    internal string _text { get; private set; } = "";

    /// <summary>
    ///     线条标注配置。
    /// </summary>
    internal string _line { get; private set; } = "";

    /// <summary>
    ///     区域标注配置。
    /// </summary>
    internal string _region { get; private set; } = "";

    /// <summary>
    ///     设置文本标注。
    /// </summary>
    /// <param name="text">标注文本内容。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public AnnotationBuilder with_text(string text)
    {
        _text = text;
        return this;
    }

    /// <summary>
    ///     设置线条标注。
    /// </summary>
    /// <param name="line_config">线条标注配置。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public AnnotationBuilder with_line(string line_config)
    {
        _line = line_config;
        return this;
    }

    /// <summary>
    ///     设置区域标注。
    /// </summary>
    /// <param name="region_config">区域标注配置。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public AnnotationBuilder with_region(string region_config)
    {
        _region = region_config;
        return this;
    }

    /// <summary>
    ///     完成标注配置，返回所属图表画布。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        return _canvas;
    }
}
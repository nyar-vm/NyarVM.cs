using Plotter.Core;

namespace Plotter.Grammar;

/// <summary>
///     布局面板构建器，用于配置多图布局方式。
/// </summary>
public sealed class LayoutPanelBuilder
{
    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     初始化 <see cref="LayoutPanelBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public LayoutPanelBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    ///     行分割数。
    /// </summary>
    internal int _row_count { get; private set; }

    /// <summary>
    ///     列分割数。
    /// </summary>
    internal int _column_count { get; private set; }

    /// <summary>
    ///     分面字段名称。
    /// </summary>
    internal string _facet_field { get; private set; } = "";

    /// <summary>
    ///     子图之间的内边距。
    /// </summary>
    internal float _padding { get; private set; } = 10.0f;

    /// <summary>
    ///     按行分割面板。
    /// </summary>
    /// <param name="count">行数。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public LayoutPanelBuilder split_by_row(int count)
    {
        _row_count = count;
        return this;
    }

    /// <summary>
    ///     按列分割面板。
    /// </summary>
    /// <param name="count">列数。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public LayoutPanelBuilder split_by_column(int count)
    {
        _column_count = count;
        return this;
    }

    /// <summary>
    ///     设置列数。
    /// </summary>
    /// <param name="count">列数。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public LayoutPanelBuilder with_column_count(int count)
    {
        _column_count = count;
        return this;
    }

    /// <summary>
    ///     设置分面字段，按该字段的值拆分数据为多个子图。
    /// </summary>
    /// <param name="field_name">分面字段名称。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public LayoutPanelBuilder with_facet_field(string field_name)
    {
        _facet_field = field_name;
        return this;
    }

    /// <summary>
    ///     设置子图之间的内边距。
    /// </summary>
    /// <param name="padding">内边距值。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public LayoutPanelBuilder with_padding(float padding)
    {
        _padding = padding;
        return this;
    }

    /// <summary>
    ///     完成布局面板配置，返回所属图表画布。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        return _canvas;
    }
}
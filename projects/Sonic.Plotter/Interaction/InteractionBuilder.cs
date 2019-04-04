using System;
using Plotter.Core;

namespace Plotter.Interaction;

/// <summary>
///     交互构建器，用于配置图表的交互行为和回调。
/// </summary>
public sealed class InteractionBuilder
{
    /// <summary>
    ///     所属的图表画布实例。
    /// </summary>
    private readonly ChartCanvas _canvas;

    /// <summary>
    ///     初始化 <see cref="InteractionBuilder" /> 类的新实例。
    /// </summary>
    /// <param name="canvas">所属的图表画布。</param>
    public InteractionBuilder(ChartCanvas canvas)
    {
        _canvas = canvas;
    }

    /// <summary>
    ///     交互模式。
    /// </summary>
    internal InteractionMode _mode { get; private set; }

    /// <summary>
    ///     选中回调。
    /// </summary>
    internal Action<object>? _on_selected { get; private set; }

    /// <summary>
    ///     悬停回调。
    /// </summary>
    internal Action<object>? _on_hover { get; private set; }

    /// <summary>
    ///     设置交互模式。
    /// </summary>
    /// <param name="mode">交互模式。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public InteractionBuilder with_mode(InteractionMode mode)
    {
        _mode = mode;
        return this;
    }

    /// <summary>
    ///     设置选中回调。
    /// </summary>
    /// <param name="callback">选中时的回调函数。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public InteractionBuilder on_selected(Action<object> callback)
    {
        _on_selected = callback;
        return this;
    }

    /// <summary>
    ///     设置悬停回调。
    /// </summary>
    /// <param name="callback">悬停时的回调函数。</param>
    /// <returns>当前构建器实例，支持链式调用。</returns>
    public InteractionBuilder on_hover(Action<object> callback)
    {
        _on_hover = callback;
        return this;
    }

    /// <summary>
    ///     完成交互配置，返回所属图表画布。
    /// </summary>
    /// <returns>所属的图表画布实例。</returns>
    public ChartCanvas build()
    {
        return _canvas;
    }
}
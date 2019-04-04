namespace Plotter.Interaction;

/// <summary>
///     交互模式枚举，定义图表支持的交互方式。
/// </summary>
public enum InteractionMode
{
    /// <summary>
    ///     悬停提示。
    /// </summary>
    HoverTooltip,

    /// <summary>
    ///     点击选择。
    /// </summary>
    ClickSelect,

    /// <summary>
    ///     框选。
    /// </summary>
    BoxSelect,

    /// <summary>
    ///     滚轮缩放。
    /// </summary>
    ZoomScroll,

    /// <summary>
    ///     拖拽平移。
    /// </summary>
    DragPan
}
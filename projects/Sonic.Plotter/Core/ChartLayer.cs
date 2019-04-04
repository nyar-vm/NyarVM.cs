using System;
using Plotter.Rendering;

namespace Plotter.Core;

/// <summary>
///     图层抽象基类，所有可见图表元素继承此类。
///     提供脏标记机制和渲染调度能力。
/// </summary>
public abstract class ChartLayer
{
    /// <summary>
    ///     图层唯一标识符。
    /// </summary>
    public Guid layer_id { get; } = Guid.NewGuid();

    /// <summary>
    ///     脏标记，指示图层是否需要重新渲染。
    ///     数据更新后自动设为 true，渲染完成后重置为 false。
    /// </summary>
    public bool is_dirty { get; set; } = true;

    /// <summary>
    ///     图层渲染顺序，值越大越靠前渲染。
    /// </summary>
    public int z_index { get; set; }

    /// <summary>
    ///     图层不透明度，范围 [0, 1]，其中 1 表示完全不透明。
    /// </summary>
    public double opacity { get; set; } = 1.0;

    /// <summary>
    ///     更新图层数据，自动将脏标记设为 true。
    /// </summary>
    /// <param name="data">要更新的数据对象。</param>
    public abstract void update(object data);

    /// <summary>
    ///     将图层内容渲染到指定画布上，渲染完成后将脏标记重置为 false。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    public abstract void render(IRenderCanvas canvas);

    /// <summary>
    ///     标记图层为脏，需要在下次渲染时重新绘制。
    /// </summary>
    public void mark_dirty()
    {
        is_dirty = true;
    }
}
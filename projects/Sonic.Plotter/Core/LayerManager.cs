using System;
using System.Collections.Generic;
using Plotter.Rendering;

namespace Plotter.Core;

/// <summary>
///     图层管理器，负责图层的增删、排序和渲染调度。
///     支持按 z_index 排序渲染和脏标记增量重绘。
/// </summary>
internal class LayerManager
{
    /// <summary>
    ///     管理的图层列表。
    /// </summary>
    private readonly List<ChartLayer> _layers = [];

    /// <summary>
    ///     获取当前管理的图层数量。
    /// </summary>
    public int count => _layers.Count;

    /// <summary>
    ///     添加图层到管理器中。
    /// </summary>
    /// <param name="layer">要添加的图层。</param>
    public void add_layer(ChartLayer layer)
    {
        _layers.Add(layer);
    }

    /// <summary>
    ///     从管理器中移除指定标识符的图层。
    /// </summary>
    /// <param name="layer_id">要移除的图层标识符。</param>
    /// <returns>若成功移除返回 true，否则返回 false。</returns>
    public bool remove_layer(Guid layer_id)
    {
        for (var i = 0; i < _layers.Count; i++)
            if (_layers[i].layer_id == layer_id)
            {
                _layers.RemoveAt(i);
                return true;
            }

        return false;
    }

    /// <summary>
    ///     标记指定标识符的图层为脏。
    /// </summary>
    /// <param name="layer_id">要标记的图层标识符。</param>
    public void mark_dirty(Guid layer_id)
    {
        foreach (var layer in _layers)
            if (layer.layer_id == layer_id)
            {
                layer.is_dirty = true;
                return;
            }
    }

    /// <summary>
    ///     标记所有图层为脏。
    /// </summary>
    public void mark_all_dirty()
    {
        foreach (var layer in _layers) layer.is_dirty = true;
    }

    /// <summary>
    ///     渲染所有脏图层到指定画布，按 z_index 升序排列。
    ///     渲染完成后将各图层的脏标记重置为 false。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    public void render_all(IRenderCanvas canvas)
    {
        _layers.Sort((a, b) => a.z_index.CompareTo(b.z_index));

        foreach (var layer in _layers)
            if (layer.is_dirty)
            {
                layer.render(canvas);
                layer.is_dirty = false;
            }
    }

    /// <summary>
    ///     强制渲染所有图层（无论脏标记状态），按 z_index 升序排列。
    /// </summary>
    /// <param name="canvas">渲染目标画布。</param>
    public void render_all_force(IRenderCanvas canvas)
    {
        _layers.Sort((a, b) => a.z_index.CompareTo(b.z_index));

        foreach (var layer in _layers)
        {
            layer.render(canvas);
            layer.is_dirty = false;
        }
    }

    /// <summary>
    ///     获取所有指定类型的图层。
    /// </summary>
    /// <typeparam name="T">图层类型。</typeparam>
    /// <returns>匹配类型的图层列表。</returns>
    public List<T> get_layers_of_type<T>() where T : ChartLayer
    {
        var result = new List<T>();

        foreach (var layer in _layers)
            if (layer is T typed)
                result.Add(typed);

        return result;
    }
}
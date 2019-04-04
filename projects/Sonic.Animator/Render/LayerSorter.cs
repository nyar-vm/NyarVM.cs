using System;
using System.Collections.Generic;

namespace Animator.Render;

/// <summary>
///     图层排序器（层级排序、遮罩裁剪、坐标矩阵转换）
/// </summary>
public sealed class LayerSorter
{
    private readonly List<RenderLayer> _layers = [];

    /// <summary>当前图层数量</summary>
    public int layer_count => _layers.Count;

    /// <summary>添加渲染图层</summary>
    public void add_layer(string layerName, int zIndex, float opacity, Rect2D clipRect)
    {
        _layers.Add(new RenderLayer(layerName, zIndex, opacity, clipRect));
        sort_layers();
    }

    /// <summary>移除指定名称的图层</summary>
    public bool remove_layer(string layerName)
    {
        for (var index = _layers.Count - 1; index >= 0; index--)
            if (_layers[index].layer_name == layerName)
            {
                _layers.RemoveAt(index);
                return true;
            }

        return false;
    }

    /// <summary>获取按 Z 序排列的图层列表</summary>
    public IReadOnlyList<RenderLayer> get_sorted_layers()
    {
        return _layers;
    }

    /// <summary>
    ///     将局部坐标转换为世界坐标
    /// </summary>
    /// <param name="localX">局部 X 坐标</param>
    /// <param name="localY">局部 Y 坐标</param>
    /// <param name="translateX">平移 X</param>
    /// <param name="translateY">平移 Y</param>
    /// <param name="scaleRatio">缩放比例</param>
    /// <param name="rotation">旋转角度（弧度）</param>
    public static (float WorldX, float WorldY) transform_coordinate(float localX, float localY, float translateX,
        float translateY, float scaleRatio, float rotation)
    {
        var scaledX = localX * scaleRatio;
        var scaledY = localY * scaleRatio;
        var cos = (float)Math.Cos(rotation);
        var sin = (float)Math.Sin(rotation);
        var rotatedX = scaledX * cos - scaledY * sin;
        var rotatedY = scaledX * sin + scaledY * cos;
        return (rotatedX + translateX, rotatedY + translateY);
    }

    private void sort_layers()
    {
        _layers.Sort((left, right) => left.z_index.CompareTo(right.z_index));
    }
}

/// <summary>
///     渲染图层
/// </summary>
public sealed class RenderLayer
{
    public RenderLayer(string layerName, int zIndex, float opacity, Rect2D clipRect)
    {
        layer_name = layerName;
        z_index = zIndex;
        this.opacity = opacity;
        clip_rect = clipRect;
    }

    /// <summary>图层名称</summary>
    public string layer_name { get; }

    /// <summary>层级顺序</summary>
    public int z_index { get; }

    /// <summary>不透明度</summary>
    public float opacity { get; }

    /// <summary>裁剪矩形</summary>
    public Rect2D clip_rect { get; }
}

/// <summary>
///     二维矩形区域
/// </summary>
public readonly struct Rect2D
{
    /// <summary>左上角 X 坐标</summary>
    public readonly float position_x;

    /// <summary>左上角 Y 坐标</summary>
    public readonly float position_y;

    /// <summary>宽度</summary>
    public readonly float width;

    /// <summary>高度</summary>
    public readonly float height;

    public Rect2D(float positionX, float positionY, float width, float height)
    {
        position_x = positionX;
        position_y = positionY;
        this.width = width;
        this.height = height;
    }

    /// <summary>空矩形</summary>
    public static Rect2D empty => new(0f, 0f, 0f, 0f);
}
using System;
using System.Collections.Generic;
using Plotter.Rendering;

namespace Animator.Render;

/// <summary>
///     通用动画渲染器（复用 IRenderCanvas 绘制接口）
/// </summary>
public sealed class AnimationRenderer
{
    private float _global_opacity = 1f;

    /// <summary>全局不透明度</summary>
    public float global_opacity
    {
        get => _global_opacity;
        set => _global_opacity = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    ///     批量绘制矩形区域
    /// </summary>
    /// <param name="canvas">目标画布</param>
    /// <param name="rectangles">矩形列表（X、Y、宽度、高度）</param>
    /// <param name="color">填充颜色</param>
    public void draw_rectangles(IRenderCanvas canvas,
        IReadOnlyList<(float X, float Y, float Width, float Height)> rectangles, PlotColor color)
    {
        canvas.set_fill_color(new PlotColor(color.r, color.g, color.b, (byte)(color.a * _global_opacity)));
        canvas.begin_path();

        for (var index = 0; index < rectangles.Count; index++)
        {
            var rect = rectangles[index];
            canvas.fill_rect(rect.X, rect.Y, rect.Width, rect.Height);
        }
    }

    /// <summary>
    ///     绘制带透明度混合的纹理区域
    /// </summary>
    /// <param name="canvas">目标画布</param>
    /// <param name="positionX">目标位置 X</param>
    /// <param name="positionY">目标位置 Y</param>
    /// <param name="width">绘制宽度</param>
    /// <param name="height">绘制高度</param>
    /// <param name="opacity">局部不透明度</param>
    public void draw_textured_region(IRenderCanvas canvas, float positionX, float positionY, float width, float height,
        float opacity)
    {
        var combinedOpacity = _global_opacity * opacity;
        var alpha = (byte)(combinedOpacity * 255f);
        canvas.set_fill_color(new PlotColor(255, 255, 255, alpha));
        canvas.fill_rect(positionX, positionY, width, height);
    }
}
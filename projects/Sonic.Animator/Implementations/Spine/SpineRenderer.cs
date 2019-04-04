using Plotter.Rendering;

namespace Animator.Implementations.Spine;

/// <summary>
///     Spine 专属渲染逻辑
/// </summary>
public static class SpineRenderer
{
    /// <summary>
    ///     更新骨骼变换
    /// </summary>
    /// <param name="deltaTime">帧间隔时间</param>
    public static void update_bones(float deltaTime)
    {
    }

    /// <summary>
    ///     渲染 Spine 动画到画布
    /// </summary>
    /// <param name="canvas">目标画布</param>
    /// <param name="positionX">水平位置</param>
    /// <param name="positionY">垂直位置</param>
    /// <param name="scaleRatio">缩放比例</param>
    /// <param name="opacity">不透明度</param>
    public static void render_to_canvas(IRenderCanvas canvas, float positionX, float positionY, float scaleRatio,
        float opacity)
    {
        var alpha = (byte)(opacity * 255f);
        canvas.set_fill_color(new PlotColor(255, 255, 255, alpha));
    }
}
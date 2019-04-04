using Core.Graphic.Canvas;

namespace Core.Graphic.Chart;

/// <summary>
///     IChart 接口
/// </summary>
public interface IChart
{
    /// <summary>
    ///     在画布上渲染图表
    /// </summary>
    void render(ICanvas canvas);
}
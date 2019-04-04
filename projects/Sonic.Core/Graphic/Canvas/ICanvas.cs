namespace Core.Graphic.Canvas;

/// <summary>
///     定义画布组件的基本接口。实现此接口的类型可参与画布渲染管线。
/// </summary>
public interface ICanvas
{
    /// <summary>
    ///     获取画布宽度。
    /// </summary>
    int width { get; }

    /// <summary>
    ///     获取画布高度。
    /// </summary>
    int height { get; }

    /// <summary>
    ///     清空画布内容。
    /// </summary>
    void clear();
}
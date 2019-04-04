namespace Core.Graphic.Canvas;

/// <summary>
///     IPath 接口
/// </summary>
public interface IPath
{
    /// <summary>
    ///     移动到指定坐标
    /// </summary>
    void move_to(float x, float y);

    /// <summary>
    ///     画线到指定坐标
    /// </summary>
    void line_to(float x, float y);

    /// <summary>
    ///     闭合路径
    /// </summary>
    void close();
}
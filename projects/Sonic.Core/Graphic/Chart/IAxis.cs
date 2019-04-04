namespace Core.Graphic.Chart;

/// <summary>
///     定义图表坐标轴接口。实现此接口的类型可提供坐标轴标签信息。
/// </summary>
public interface IAxis
{
    /// <summary>
    ///     获取坐标轴标签。
    /// </summary>
    string label { get; }
}
namespace Core.Graphic.Chart;

/// <summary>
///     定义图表图例接口。实现此接口的类型可提供图例标题信息。
/// </summary>
public interface ILegend
{
    /// <summary>
    ///     获取图例标题。
    /// </summary>
    string title { get; }
}
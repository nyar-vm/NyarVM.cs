namespace Core.Graphic.Chart;

/// <summary>
///     定义图表数据系列接口。实现此接口的类型可提供系列名称信息。
/// </summary>
public interface ISeries
{
    /// <summary>
    ///     获取数据系列名称。
    /// </summary>
    string name { get; }
}
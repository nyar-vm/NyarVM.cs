namespace Core.Math.Graph;

/// <summary>
///     边索引接口，提供图中边的整数值索引。
/// </summary>
public interface IEdgeIndex
{
    /// <summary>
    ///     获取边的整数值索引。
    /// </summary>
    int value { get; }
}
namespace Std.Math.GraphTheory.Graph;

/// <summary>
///     图标记接口，提供节点和边的计数属性用于类型擦除引用。
/// </summary>
public interface IGraph
{
    /// <summary>
    ///     获取图中节点的数量。
    /// </summary>
    int NodeCount { get; }

    /// <summary>
    ///     获取图中边的数量。
    /// </summary>
    int EdgeCount { get; }
}
namespace Core.Math.Tensor;

/// <summary>
///     形状接口，描述张量的维度信息。
/// </summary>
public interface IShape
{
    /// <summary>
    ///     获取张量的维度数（秩）。
    /// </summary>
    int rank { get; }

    /// <summary>
    ///     获取指定维度的大小。
    /// </summary>
    /// <param name="dimension">维度索引。</param>
    /// <returns>该维度的大小。</returns>
    int this[int dimension] { get; }

    /// <summary>
    ///     获取张量的元素总数。
    /// </summary>
    int total_elements { get; }
}
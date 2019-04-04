namespace Core.Math.Tensor;

/// <summary>
///     张量接口，定义多维数组的形状和元素访问方式。
/// </summary>
/// <typeparam name="T">张量元素的类型。</typeparam>
public interface ITensor<T>
{
    /// <summary>
    ///     获取张量的形状。
    /// </summary>
    IShape shape { get; }

    /// <summary>
    ///     获取或设置指定多维索引处的元素。
    /// </summary>
    /// <param name="indices">多维索引。</param>
    /// <returns>指定索引处的元素值。</returns>
    T this[params int[] indices] { get; set; }
}
namespace Core.Math.Linear;

/// <summary>
///     向量接口，定义按索引访问元素和获取维度的方式。
/// </summary>
/// <typeparam name="T">向量元素的类型。</typeparam>
/// <typeparam name="D">维度标记类型。</typeparam>
public interface IVector<T, TD> where T : struct
{
    /// <summary>
    ///     获取或设置指定索引处的元素。
    /// </summary>
    /// <param name="index">元素索引。</param>
    /// <returns>指定索引处的元素值。</returns>
    T this[int index] { get; set; }

    /// <summary>
    ///     获取向量的维度。
    /// </summary>
    int dimension { get; }
}
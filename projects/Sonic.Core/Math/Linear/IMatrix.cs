namespace Core.Math.Linear;

/// <summary>
///     矩阵接口，定义按行列访问元素和获取矩阵尺寸的方式。
/// </summary>
/// <typeparam name="T">矩阵元素的类型。</typeparam>
/// <typeparam name="R">行数标记类型。</typeparam>
/// <typeparam name="C">列数标记类型。</typeparam>
public interface IMatrix<T, TR, TC> where T : struct
{
    /// <summary>
    ///     获取或设置指定行列位置的元素。
    /// </summary>
    /// <param name="row">行索引。</param>
    /// <param name="col">列索引。</param>
    /// <returns>指定位置的元素值。</returns>
    T this[int row, int col] { get; set; }

    /// <summary>
    ///     获取矩阵的行数。
    /// </summary>
    int rows { get; }

    /// <summary>
    ///     获取矩阵的列数。
    /// </summary>
    int columns { get; }
}
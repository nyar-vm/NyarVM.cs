namespace Core.Math.Tensor;

/// <summary>
///     损失函数接口，定义预测值与真实值之间的损失计算方式。
/// </summary>
/// <typeparam name="T">损失值的类型。</typeparam>
public interface ILoss<T>
{
    /// <summary>
    ///     计算预测张量与真实张量之间的损失值。
    /// </summary>
    /// <param name="predicted">预测张量。</param>
    /// <param name="actual">真实张量。</param>
    /// <returns>损失值。</returns>
    T compute(ITensor<T> predicted, ITensor<T> actual);
}
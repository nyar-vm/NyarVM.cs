namespace Core.Math;

/// <summary>
///     可加性接口，定义类型的加法运算。
/// </summary>
/// <typeparam name="T">实现此接口的类型本身。</typeparam>
public interface IAdditive<T> where T : IAdditive<T>
{
    /// <summary>
    ///     计算两个值的加法结果。
    /// </summary>
    /// <param name="left">左操作数。</param>
    /// <param name="right">右操作数。</param>
    /// <returns>加法结果。</returns>
    static abstract T operator +(T left, T right);
}
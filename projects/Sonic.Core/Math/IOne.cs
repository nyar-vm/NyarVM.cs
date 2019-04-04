namespace Core.Math;

/// <summary>
///     幺元素接口，提供类型的乘法单位元。
/// </summary>
/// <typeparam name="T">实现此接口的类型本身。</typeparam>
public interface IOne<T>
{
    /// <summary>
    ///     获取类型的幺元素（乘法单位元）。
    /// </summary>
    static abstract T one { get; }
}
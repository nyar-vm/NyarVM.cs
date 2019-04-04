namespace Core.Math;

/// <summary>
///     零元素接口，提供类型的加法单位元。
/// </summary>
/// <typeparam name="T">实现此接口的类型本身。</typeparam>
public interface IZero<T>
{
    /// <summary>
    ///     获取类型的零元素（加法单位元）。
    /// </summary>
    static abstract T zero { get; }
}
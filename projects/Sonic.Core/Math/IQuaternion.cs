namespace Core.Math;

/// <summary>
///     四元数接口，定义四元数的四个分量访问方式。
/// </summary>
/// <typeparam name="T">四元数分量的数值类型。</typeparam>
public interface IQuaternion<T>
{
    /// <summary>
    ///     获取四元数的 W 分量（实部）。
    /// </summary>
    T w { get; }

    /// <summary>
    ///     获取四元数的 X 分量。
    /// </summary>
    T x { get; }

    /// <summary>
    ///     获取四元数的 Y 分量。
    /// </summary>
    T y { get; }

    /// <summary>
    ///     获取四元数的 Z 分量。
    /// </summary>
    T z { get; }
}
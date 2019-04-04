namespace Core.Math;

/// <summary>
///     变换接口，定义平移、旋转和缩放操作。
/// </summary>
/// <typeparam name="T">变换的数值类型。</typeparam>
public interface ITransform<T>
{
    /// <summary>
    ///     沿指定偏移量进行平移。
    /// </summary>
    /// <param name="offset">平移偏移量。</param>
    /// <returns>平移后的结果。</returns>
    T translate(T offset);

    /// <summary>
    ///     按指定角度进行旋转。
    /// </summary>
    /// <param name="angle">旋转角度。</param>
    /// <returns>旋转后的结果。</returns>
    T rotate(T angle);

    /// <summary>
    ///     按指定因子进行缩放。
    /// </summary>
    /// <param name="factor">缩放因子。</param>
    /// <returns>缩放后的结果。</returns>
    T scale(T factor);
}
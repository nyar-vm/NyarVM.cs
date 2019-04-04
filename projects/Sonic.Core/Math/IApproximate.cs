namespace Core.Math;

/// <summary>
///     近似相等接口，支持在给定容差范围内比较两个值是否近似相等。
/// </summary>
/// <typeparam name="T">实现此接口的类型本身。</typeparam>
public interface IApproximate<T>
{
    /// <summary>
    ///     判断当前值与另一个值是否在指定容差范围内近似相等。
    /// </summary>
    /// <param name="other">待比较的另一个值。</param>
    /// <param name="epsilon">容差范围。</param>
    /// <returns>若近似相等则返回 <c>true</c>，否则返回 <c>false</c>。</returns>
    bool approx_equals(T other, T epsilon);
}
namespace Std.DataProcess.Deserialize;

/// <summary>
///     数组反序列化子读取器，提供对数组元素的顺序读取�?/// 通过 <see cref="IDeserializer.deserialize_sequence" /> 获取�?///
/// </summary>
public interface IArrayDeserializer : IDisposable
{
    /// <summary>
    ///     尝试读取下一个数组元素�?    ///
    /// </summary>
    /// <param name="deserializer">
    ///     用于读取元素的反序列化器�?/param>
    ///     <returns>如果还有更多元素返回 <c>true</c>，数组结束返�?<c>false</c>�?/returns>
    bool try_read_element(IDeserializer deserializer);

    /// <summary>
    ///     结束当前数组结构的读取�?    ///
    /// </summary>
    void end();
}
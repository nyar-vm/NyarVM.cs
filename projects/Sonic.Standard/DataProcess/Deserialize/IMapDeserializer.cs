using Std.Category;

namespace Std.DataProcess.Deserialize;

/// <summary>
///     对象反序列化子读取器，提供对对象字段的顺序读取�?/// 通过 <see cref="IDeserializer.deserialize_map" /> 获取�?///
/// </summary>
public interface IMapDeserializer : IDisposable
{
    /// <summary>
    ///     尝试读取下一个字段名�?    ///
    /// </summary>
    /// <param name="name">
    ///     读取到的字段名称�?/param>
    ///     <returns>如果还有更多字段返回 <c>true</c>，对象结束返�?<c>false</c>�?/returns>
    Result<string, DeserializeException> read_field_name();

    /// <summary>
    ///     使用反序列化器读取字段值�?    ///
    /// </summary>
    /// <param name="deserializer">用于读取值的反序列化器�?/param>
    void deserialize_value(IDeserializer deserializer);

    /// <summary>
    ///     使用类型反序列化器读取字段值�?    ///
    /// </summary>
    /// <param name="deserialize">
    ///     类型反序列化器�?/param>
    ///     <typeparam name="T">
    ///         值的类型�?/typeparam>
    ///         <returns>读取到的值�?/returns>
    T deserialize_value<T>(IDeserialize<T> deserialize);

    /// <summary>
    ///     结束当前对象结构的读取�?    ///
    /// </summary>
    void end();
}
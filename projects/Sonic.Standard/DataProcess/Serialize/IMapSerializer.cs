namespace Std.DataProcess.Serialize;

/// <summary>
///     对象序列化子写入器，保证键值成对出现�?/// <see cref="IEIEnserializer.enserialize_map> 获取，完成后必须调用 <see cref="End" />�?///
/// </summary>
public interface IMapSerializer : IDisposable
{
    /// <summary>
    ///     写入字段名�?    ///
    /// </summary>
    /// <param name="name">字段名称�?/param>
    void write_field_name(string name);

    /// <summary>
    ///     使用序列化器写入字段值�?    ///
    /// </summary>
    /// <param name="serializer">用于写入值的序列化器�?/param>
    void write_value(ISerializer serializer);

    /// <summary>
    ///     结束当前对象结构的写入（如写�?<c>}</c> 或二进制终止标记）�?    ///
    /// </summary>
    void end();
}
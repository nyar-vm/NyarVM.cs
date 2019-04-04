namespace Std.DataProcess.Serialize;

/// <summary>
///     数组序列化子写入器，用于写入数组元素�?/// <see crefISerializer.serialize_sequencece"/> 获取，完成后必须调用 <see cref="end" />�?///
/// </summary>
public interface ISequenceSerializer : IDisposable
{
    /// <summary>
    ///     使用序列化器写入一个数组元素�?    ///
    /// </summary>
    /// <param name="serializer">用于写入元素的序列化器�?/param>
    void write_element(ISerializer serializer);

    /// <summary>
    ///     结束当前数组结构的写入（如写�?<c>]</c> 或二进制终止标记）�?    ///
    /// </summary>
    void end();
}
namespace Std.DataProcess.Write;

/// <summary>
///     缓冲区写入器接口，语义上等价�?<c>System.Buffers.IBufferWriter&lt;T&gt;</c>�?/// 提供对连续内存区域的按需获取与提交机制，避免中间缓冲区拷贝�?///
/// </summary>
/// <typeparam name="T">写入元素的类型�?/typeparam>
public interface IBufferWriter<T>
{
    /// <summary>
    ///     通知写入器已向之前获取的缓冲区中写入�?<paramref name="count" /> 个元素�?    ///
    /// </summary>
    /// <param name="count">已写入的元素数量�?/param>
    void advance(int count);

    /// <summary>
    ///     获取至少能容�?<paramref name="sizeHint" /> 个元素的 <see cref="Memory{T}" />�?    ///
    /// </summary>
    /// <param name="sizeHint">
    ///     请求的最小元素数�? 表示使用默认大小�?/param>
    ///     <returns>可写入的内存区域�?/returns>
    Memory<T> get_memory(int sizeHint = 0);

    /// <summary>
    ///     获取至少能容�?<paramref name="sizeHint" /> 个元素的 <see cref="Span{T}" />�?    ///
    /// </summary>
    /// <param name="sizeHint">
    ///     请求的最小元素数�? 表示使用默认大小�?/param>
    ///     <returns>可写入的跨度区域�?/returns>
    Span<T> get_span(int sizeHint = 0);
}
namespace Std.DataProcess.Read;

/// <summary>
///     缓冲区读取器接口，对称于 <see cref="IBufferWriter{T}" />�?/// 提供对已读入数据的视图获取与消费机制，避免中间拷贝�?///
/// </summary>
/// <typeparam name="T">读取元素的类型�?/typeparam>
public interface IBufferReader<T>
{
    /// <summary>
    ///     通知读取器已消费了之前获取的缓冲区中�?<paramref name="count" /> 个元素�?    ///
    /// </summary>
    /// <param name="count">已消费的元素数量�?/param>
    void advance(int count);

    /// <summary>
    ///     获取当前可读取数据的 <see cref="ReadOnlyMemory{T}" /> 视图�?    ///
    /// </summary>
    /// <param name="sizeHint">
    ///     期望的最小可读元素数�? 表示不检查�?/param>
    ///     <returns>可读取的内存区域�?/returns>
    ReadOnlyMemory<T> get_memory(int sizeHint = 0);

    /// <summary>
    ///     获取当前可读取数据的 <see cref="ReadOnlySpan{T}" /> 视图�?    ///
    /// </summary>
    /// <param name="sizeHint">
    ///     期望的最小可读元素数�? 表示不检查�?/param>
    ///     <returns>可读取的跨度区域�?/returns>
    ReadOnlySpan<T> get_span(int sizeHint = 0);
}
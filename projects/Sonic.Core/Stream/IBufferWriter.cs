using System;

namespace Core.Stream;

/// <summary>
///     缓冲区写入器接口，支持获取连续内存跨度并推进写入位置
/// </summary>
/// <typeparam name="T">元素类型</typeparam>
public interface IBufferWriter<T>
{
    /// <summary>
    ///     获取可用于写入的内存跨度
    /// </summary>
    /// <param name="sizeHint">请求的最小大小，0 表示使用默认大小</param>
    /// <returns>可写入的内存跨度</returns>
    Span<T> get_span(int sizeHint = 0);

    /// <summary>
    ///     通知写入器已写入指定数量的元素
    /// </summary>
    /// <param name="count">已写入的元素数量</param>
    void advance(int count);
}
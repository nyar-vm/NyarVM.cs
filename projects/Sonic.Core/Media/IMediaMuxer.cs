namespace Core.Media;

/// <summary>
///     媒体复用器接口，将多个编码流合并为容器格式。
/// </summary>
public interface IMediaMuxer
{
    /// <summary>
    ///     添加编码流到复用器。
    /// </summary>
    /// <param name="encoder">媒体编码器。</param>
    void add_stream(IMediaEncoder encoder);

    /// <summary>
    ///     执行复用操作，输出合并后的数据。
    /// </summary>
    /// <returns>复用后的字节数组。</returns>
    byte[] mux();
}
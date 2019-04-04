namespace Core.Media;

/// <summary>
///     媒体编码器接口，将原始媒体数据编码为目标格式。
/// </summary>
public interface IMediaEncoder
{
    /// <summary>
    ///     编码当前媒体数据。
    /// </summary>
    /// <returns>编码后的字节数组。</returns>
    byte[] encode();
}
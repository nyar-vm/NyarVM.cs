namespace Core.Media;

/// <summary>
///     媒体解码器接口，将编码数据解码为原始媒体数据。
/// </summary>
public interface IMediaDecoder
{
    /// <summary>
    ///     解码输入数据。
    /// </summary>
    /// <param name="input">编码后的输入数据。</param>
    /// <returns>解码是否成功。</returns>
    bool decode(byte[] input);
}
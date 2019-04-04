namespace Core.Media;

/// <summary>
///     媒体解复用器接口，从容器格式中分离出各个编码流。
/// </summary>
public interface IMediaDemuxer
{
    /// <summary>
    ///     获取流数量。
    /// </summary>
    int stream_count { get; }

    /// <summary>
    ///     获取指定索引的解码器。
    /// </summary>
    /// <param name="index">流索引。</param>
    /// <returns>对应流的媒体解码器。</returns>
    IMediaDecoder get_decoder(int index);
}
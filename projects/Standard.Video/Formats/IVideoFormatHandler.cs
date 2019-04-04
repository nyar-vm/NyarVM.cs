using Core.Media.Video;

namespace Std.Video.Formats;

/// <summary>
///     视频格式处理器接口，提供视频帧的解码和编码能力。
/// </summary>
public interface IVideoFormatHandler
{
    /// <summary>
    ///     获取格式名称。
    /// </summary>
    string format_name { get; }

    /// <summary>
    ///     获取支持的文件扩展名集合。
    /// </summary>
    IEnumerable<string> file_extensions { get; }

    /// <summary>
    ///     解码指定帧索引的视频帧。
    /// </summary>
    /// <param name="data">编码后的视频数据。</param>
    /// <param name="frameIndex">帧索引。</param>
    /// <returns>解码后的视频帧。</returns>
    IVideoFrame decode_frame(ReadOnlySpan<byte> data, int frameIndex);

    /// <summary>
    ///     编码视频片段为目标格式。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="clip">视频片段。</param>
    /// <returns>编码后的字节数组。</returns>
    byte[] encode<TPixel>(VideoClip<TPixel> clip) where TPixel : unmanaged;
}
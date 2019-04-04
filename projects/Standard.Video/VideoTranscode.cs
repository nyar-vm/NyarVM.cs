using Sonic.Audio;
using Std.Image.Pixels;

namespace Std.Video;

/// <summary>
///     视频转码静态类，提供视频解码、编码和音视频复用的高层门面。
/// </summary>
public static class VideoTranscode
{
    /// <summary>
    ///     将编码后的视频数据解码为视频片段。
    /// </summary>
    /// <param name="data">编码后的视频数据。</param>
    /// <returns>解码后的视频片段。</returns>
    public static VideoClip<Rgba32> decode(ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     将视频片段编码为目标格式。
    /// </summary>
    /// <typeparam name="TPixel">像素类型。</typeparam>
    /// <param name="clip">视频片段。</param>
    /// <param name="options">编码选项。</param>
    /// <returns>编码后的字节数组。</returns>
    public static byte[] encode<TPixel>(VideoClip<TPixel> clip, VideoEncodeOptions options)
        where TPixel : unmanaged
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     将视频和音频复用为容器格式。
    /// </summary>
    /// <param name="video">视频片段。</param>
    /// <param name="audio">音频帧，可为 null。</param>
    /// <param name="options">复用选项。</param>
    /// <returns>复用后的字节数组。</returns>
    public static byte[] mux(VideoClip<Rgba32> video, AudioFrame<float>? audio, MuxOptions options)
    {
        throw new NotImplementedException();
    }
}
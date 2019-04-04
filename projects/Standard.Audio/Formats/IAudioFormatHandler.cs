using Core.Media.Audio;

namespace Sonic.Audio.Formats;

/// <summary>
///     音频格式处理器接口，定义音频数据的加载和保存能力。
/// </summary>
public interface IAudioFormatHandler
{
    /// <summary>
    ///     获取格式名称。
    /// </summary>
    string format_name { get; }

    /// <summary>
    ///     获取支持的文件扩展名列表。
    /// </summary>
    IEnumerable<string> file_extensions { get; }

    /// <summary>
    ///     从二进制数据加载音频。
    /// </summary>
    /// <param name="data">音频二进制数据。</param>
    /// <returns>加载后的音频对象。</returns>
    IAudio load(ReadOnlySpan<byte> data);

    /// <summary>
    ///     将音频帧保存为二进制数据。
    /// </summary>
    /// <typeparam name="TSample">采样数据类型。</typeparam>
    /// <param name="audio">音频帧。</param>
    /// <returns>编码后的二进制数据。</returns>
    byte[] save<TSample>(AudioFrame<TSample> audio) where TSample : unmanaged;
}
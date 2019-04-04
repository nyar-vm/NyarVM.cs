namespace Sonic.Audio.Extensions;

/// <summary>
///     音频处理器接口，定义音频帧的处理能力。
/// </summary>
public interface IAudioProcessor
{
    /// <summary>
    ///     获取处理器标识键。
    /// </summary>
    string key { get; }

    /// <summary>
    ///     判断是否能处理指定的音频帧。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <returns>是否能处理。</returns>
    bool can_process(AudioFrame<float> audio);

    /// <summary>
    ///     处理音频帧。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <returns>处理后的音频帧。</returns>
    AudioFrame<float> process(AudioFrame<float> audio);
}
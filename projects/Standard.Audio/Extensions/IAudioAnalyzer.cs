namespace Sonic.Audio.Extensions;

/// <summary>
///     音频分析器接口，定义音频帧的分析能力。
/// </summary>
public interface IAudioAnalyzer
{
    /// <summary>
    ///     获取分析器标识键。
    /// </summary>
    string key { get; }

    /// <summary>
    ///     判断是否能分析指定的音频帧。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <returns>是否能分析。</returns>
    bool can_analyze(AudioFrame<float> audio);

    /// <summary>
    ///     分析音频帧。
    /// </summary>
    /// <param name="audio">输入音频帧。</param>
    /// <returns>分析结果对象。</returns>
    object analyze(AudioFrame<float> audio);
}
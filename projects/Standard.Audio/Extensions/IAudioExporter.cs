namespace Sonic.Audio.Extensions;

/// <summary>
///     音频导出器接口，定义将音频帧导出到外部目标的能力。
/// </summary>
public interface IAudioExporter
{
    /// <summary>
    ///     判断是否能导出指定格式的音频。
    /// </summary>
    /// <param name="format">目标格式名称。</param>
    /// <returns>是否能导出。</returns>
    bool can_export(string format);

    /// <summary>
    ///     将音频帧导出为指定格式的二进制数据。
    /// </summary>
    /// <param name="audio">音频帧。</param>
    /// <param name="format">目标格式名称。</param>
    /// <returns>导出的二进制数据。</returns>
    byte[] export(AudioFrame<float> audio, string format);
}
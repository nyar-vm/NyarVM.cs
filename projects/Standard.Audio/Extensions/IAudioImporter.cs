namespace Sonic.Audio.Extensions;

/// <summary>
///     音频导入器接口，定义从外部数据源导入音频的能力。
/// </summary>
public interface IAudioImporter
{
    /// <summary>
    ///     判断是否能导入指定路径的音频。
    /// </summary>
    /// <param name="path">音频文件路径。</param>
    /// <returns>是否能导入。</returns>
    bool can_import(string path);

    /// <summary>
    ///     从指定路径导入音频帧。
    /// </summary>
    /// <param name="path">音频文件路径。</param>
    /// <returns>导入的音频帧。</returns>
    AudioFrame<float> import(string path);
}
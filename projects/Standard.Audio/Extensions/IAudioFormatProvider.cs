using Sonic.Audio.Formats;

namespace Sonic.Audio.Extensions;

/// <summary>
///     音频格式提供者接口，定义创建格式检测器和处理器的能力。
/// </summary>
public interface IAudioFormatProvider
{
    /// <summary>
    ///     创建格式检测器列表。
    /// </summary>
    /// <returns>格式检测器列表。</returns>
    IEnumerable<IAudioFormatDetector> create_detectors();

    /// <summary>
    ///     创建格式处理器列表。
    /// </summary>
    /// <returns>格式处理器列表。</returns>
    IEnumerable<IAudioFormatHandler> create_handlers();
}
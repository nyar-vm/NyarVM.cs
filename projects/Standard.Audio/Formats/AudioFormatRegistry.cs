namespace Sonic.Audio.Formats;

/// <summary>
///     音频格式注册表类，管理音频格式处理器和检测器的注册与查找。
/// </summary>
public sealed class AudioFormatRegistry
{
    /// <summary>
    ///     格式检测器字典，以格式名称为键。
    /// </summary>
    private readonly Dictionary<string, IAudioFormatDetector> _detectors = new();

    /// <summary>
    ///     格式处理器字典，以格式名称为键。
    /// </summary>
    private readonly Dictionary<string, IAudioFormatHandler> _handlers = new();

    /// <summary>
    ///     获取所有已注册的格式名称。
    /// </summary>
    public IEnumerable<string> supported_formats
    {
        get
        {
            var formats = new HashSet<string>();
            foreach (var name in _handlers.Keys) formats.Add(name);

            foreach (var name in _detectors.Keys) formats.Add(name);

            return formats;
        }
    }

    /// <summary>
    ///     注册音频格式处理器。
    /// </summary>
    /// <param name="handler">格式处理器。</param>
    public void register_handler(IAudioFormatHandler handler)
    {
        _handlers[handler.format_name] = handler;
    }

    /// <summary>
    ///     注册音频格式检测器。
    /// </summary>
    /// <param name="detector">格式检测器。</param>
    public void register_detector(IAudioFormatDetector detector)
    {
        _detectors[detector.format_name] = detector;
    }

    /// <summary>
    ///     根据格式名称获取格式处理器。
    /// </summary>
    /// <param name="format_name">格式名称。</param>
    /// <returns>格式处理器，若未找到则返回 null。</returns>
    public IAudioFormatHandler? get_handler(string format_name)
    {
        return _handlers.TryGetValue(format_name, out var handler) ? handler : null;
    }

    /// <summary>
    ///     根据格式名称获取格式检测器。
    /// </summary>
    /// <param name="format_name">格式名称。</param>
    /// <returns>格式检测器，若未找到则返回 null。</returns>
    public IAudioFormatDetector? get_detector(string format_name)
    {
        return _detectors.TryGetValue(format_name, out var detector) ? detector : null;
    }

    /// <summary>
    ///     通过文件头字节检测音频格式。
    /// </summary>
    /// <param name="header">文件头字节。</param>
    /// <returns>检测到的格式名称，若未识别则返回 null。</returns>
    public string? detect_format(ReadOnlySpan<byte> header)
    {
        foreach (var detector in _detectors.Values)
            if (detector.detect(header))
                return detector.format_name;

        return null;
    }
}
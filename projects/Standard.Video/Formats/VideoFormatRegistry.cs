namespace Std.Video.Formats;

/// <summary>
///     视频格式注册表，管理视频格式处理器和探测器的注册与查询。
/// </summary>
public sealed class VideoFormatRegistry
{
    private readonly List<IVideoFormatDetector> _detectors = [];
    private readonly Dictionary<string, IVideoFormatHandler> _handlers = new();

    /// <summary>
    ///     获取所有已注册的格式处理器。
    /// </summary>
    public IEnumerable<IVideoFormatHandler> handlers => _handlers.Values;

    /// <summary>
    ///     获取所有已注册的格式探测器。
    /// </summary>
    public IEnumerable<IVideoFormatDetector> detectors => _detectors;

    /// <summary>
    ///     注册视频格式处理器。
    /// </summary>
    /// <param name="handler">格式处理器。</param>
    public void register_handler(IVideoFormatHandler handler)
    {
        _handlers[handler.format_name] = handler;
    }

    /// <summary>
    ///     注册视频格式探测器。
    /// </summary>
    /// <param name="detector">格式探测器。</param>
    public void register_detector(IVideoFormatDetector detector)
    {
        _detectors.Add(detector);
    }

    /// <summary>
    ///     根据格式名称获取视频格式处理器。
    /// </summary>
    /// <param name="formatName">格式名称。</param>
    /// <returns>对应的格式处理器，若不存在则返回 null。</returns>
    public IVideoFormatHandler? get_handler(string formatName)
    {
        return _handlers.TryGetValue(formatName, out var handler) ? handler : null;
    }

    /// <summary>
    ///     根据文件头数据探测视频格式。
    /// </summary>
    /// <param name="header">文件头数据。</param>
    /// <returns>匹配的格式名称，若未匹配则返回 null。</returns>
    public string? detect_format(ReadOnlySpan<byte> header)
    {
        for (var i = 0; i < _detectors.Count; i++)
            if (_detectors[i].detect(header))
                return _detectors[i].format_name;

        return null;
    }

    /// <summary>
    ///     根据文件扩展名探测视频格式。
    /// </summary>
    /// <param name="extension">文件扩展名。</param>
    /// <returns>匹配的格式名称，若未匹配则返回 null。</returns>
    public string? detect_format(string extension)
    {
        for (var i = 0; i < _detectors.Count; i++)
            if (_detectors[i].detect(extension))
                return _detectors[i].format_name;

        return null;
    }
}
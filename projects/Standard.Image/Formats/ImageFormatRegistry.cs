namespace Std.Image.Formats;

/// <summary>
///     图像格式注册表，管理格式处理器和探测器的注册与查询。
/// </summary>
public sealed class ImageFormatRegistry
{
    private readonly Dictionary<string, IImageFormatDetector> _detectors = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, IImageFormatDetector> _extensionDetectors =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, IImageFormatHandler> _extensionHandlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IImageFormatHandler> _handlers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _supportedFormats = [];

    /// <summary>
    ///     获取支持的格式名称列表。
    /// </summary>
    public IReadOnlyList<string> supported_formats => _supportedFormats;

    /// <summary>
    ///     注册格式处理器。
    /// </summary>
    /// <param name="handler">格式处理器。</param>
    public void register_handler(IImageFormatHandler handler)
    {
        _handlers[handler.format_name] = handler;

        foreach (var ext in handler.file_extensions) _extensionHandlers[ext] = handler;

        if (!_supportedFormats.Contains(handler.format_name)) _supportedFormats.Add(handler.format_name);
    }

    /// <summary>
    ///     注册格式探测器。
    /// </summary>
    /// <param name="detector">格式探测器。</param>
    public void register_detector(IImageFormatDetector detector)
    {
        _detectors[detector.format_name] = detector;

        foreach (var ext in detector.file_extensions) _extensionDetectors[ext] = detector;

        if (!_supportedFormats.Contains(detector.format_name)) _supportedFormats.Add(detector.format_name);
    }

    /// <summary>
    ///     根据格式名称获取格式处理器。
    /// </summary>
    /// <param name="format_name">格式名称。</param>
    /// <returns>格式处理器，未找到时返回 null。</returns>
    public IImageFormatHandler? get_handler(string format_name)
    {
        return _handlers.TryGetValue(format_name, out var handler) ? handler : null;
    }

    /// <summary>
    ///     根据文件扩展名获取格式处理器。
    /// </summary>
    /// <param name="extension">文件扩展名（不含前导点号）。</param>
    /// <returns>格式处理器，未找到时返回 null。</returns>
    public IImageFormatHandler? get_handler_by_extension(string extension)
    {
        return _extensionHandlers.TryGetValue(extension, out var handler) ? handler : null;
    }

    /// <summary>
    ///     根据格式名称获取格式探测器。
    /// </summary>
    /// <param name="format_name">格式名称。</param>
    /// <returns>格式探测器，未找到时返回 null。</returns>
    public IImageFormatDetector? get_detector(string format_name)
    {
        return _detectors.TryGetValue(format_name, out var detector) ? detector : null;
    }

    /// <summary>
    ///     根据文件头字节检测格式探测器。
    /// </summary>
    /// <param name="header">文件头字节。</param>
    /// <returns>匹配的格式探测器，未匹配时返回 null。</returns>
    public IImageFormatDetector? detect_format(ReadOnlySpan<byte> header)
    {
        foreach (var detector in _detectors.Values)
            if (detector.detect(header))
                return detector;

        return null;
    }

    /// <summary>
    ///     根据文件头字节检测格式名称。
    /// </summary>
    /// <param name="header">文件头字节。</param>
    /// <returns>格式名称，未匹配时返回 null。</returns>
    public string? detect_format_name(ReadOnlySpan<byte> header)
    {
        return detect_format(header)?.format_name;
    }
}
using Sonic.Audio.Formats;

namespace Sonic.Audio.Extensions;

/// <summary>
///     音频扩展上下文类，为扩展提供注册处理器、检测器、分析器等组件的能力。
/// </summary>
public sealed class AudioExtensionContext
{
    /// <summary>
    ///     音频分析器列表。
    /// </summary>
    private readonly List<IAudioAnalyzer> _analyzers = [];

    /// <summary>
    ///     格式检测器列表。
    /// </summary>
    private readonly List<IAudioFormatDetector> _detectors = [];

    /// <summary>
    ///     音频导出器列表。
    /// </summary>
    private readonly List<IAudioExporter> _exporters = [];

    /// <summary>
    ///     格式处理器列表。
    /// </summary>
    private readonly List<IAudioFormatHandler> _handlers = [];

    /// <summary>
    ///     音频导入器列表。
    /// </summary>
    private readonly List<IAudioImporter> _importers = [];

    /// <summary>
    ///     音频处理器列表。
    /// </summary>
    private readonly List<IAudioProcessor> _processors = [];

    /// <summary>
    ///     获取所有已注册的格式处理器。
    /// </summary>
    public IReadOnlyList<IAudioFormatHandler> handlers => _handlers;

    /// <summary>
    ///     获取所有已注册的格式检测器。
    /// </summary>
    public IReadOnlyList<IAudioFormatDetector> detectors => _detectors;

    /// <summary>
    ///     获取所有已注册的音频处理器。
    /// </summary>
    public IReadOnlyList<IAudioProcessor> processors => _processors;

    /// <summary>
    ///     获取所有已注册的音频分析器。
    /// </summary>
    public IReadOnlyList<IAudioAnalyzer> analyzers => _analyzers;

    /// <summary>
    ///     获取所有已注册的音频导入器。
    /// </summary>
    public IReadOnlyList<IAudioImporter> importers => _importers;

    /// <summary>
    ///     获取所有已注册的音频导出器。
    /// </summary>
    public IReadOnlyList<IAudioExporter> exporters => _exporters;

    /// <summary>
    ///     添加格式处理器。
    /// </summary>
    /// <param name="handler">格式处理器。</param>
    public void add_handler(IAudioFormatHandler handler)
    {
        _handlers.Add(handler);
    }

    /// <summary>
    ///     添加格式检测器。
    /// </summary>
    /// <param name="detector">格式检测器。</param>
    public void add_detector(IAudioFormatDetector detector)
    {
        _detectors.Add(detector);
    }

    /// <summary>
    ///     添加音频处理器。
    /// </summary>
    /// <param name="processor">音频处理器。</param>
    public void add_processor(IAudioProcessor processor)
    {
        _processors.Add(processor);
    }

    /// <summary>
    ///     添加音频分析器。
    /// </summary>
    /// <param name="analyzer">音频分析器。</param>
    public void add_analyzer(IAudioAnalyzer analyzer)
    {
        _analyzers.Add(analyzer);
    }

    /// <summary>
    ///     添加音频导入器。
    /// </summary>
    /// <param name="importer">音频导入器。</param>
    public void add_importer(IAudioImporter importer)
    {
        _importers.Add(importer);
    }

    /// <summary>
    ///     添加音频导出器。
    /// </summary>
    /// <param name="exporter">音频导出器。</param>
    public void add_exporter(IAudioExporter exporter)
    {
        _exporters.Add(exporter);
    }
}
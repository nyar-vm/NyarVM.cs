namespace Std.Video.Extensions;

/// <summary>
///     视频扩展上下文类，提供扩展组件的注册入口。
/// </summary>
public sealed class VideoExtensionContext
{
    private readonly List<IVideoExporter> _exporters = [];
    private readonly List<IVideoFormatProvider> _format_providers = [];
    private readonly List<IVideoImporter> _importers = [];
    private readonly List<IVideoProcessor> _processors = [];
    private readonly List<IVideoVisionOperator> _vision_operators = [];

    /// <summary>
    ///     获取已注册的格式提供者。
    /// </summary>
    public IEnumerable<IVideoFormatProvider> format_providers => _format_providers;

    /// <summary>
    ///     获取已注册的处理器。
    /// </summary>
    public IEnumerable<IVideoProcessor> processors => _processors;

    /// <summary>
    ///     获取已注册的视觉算子。
    /// </summary>
    public IEnumerable<IVideoVisionOperator> vision_operators => _vision_operators;

    /// <summary>
    ///     获取已注册的导入器。
    /// </summary>
    public IEnumerable<IVideoImporter> importers => _importers;

    /// <summary>
    ///     获取已注册的导出器。
    /// </summary>
    public IEnumerable<IVideoExporter> exporters => _exporters;

    /// <summary>
    ///     添加格式提供者。
    /// </summary>
    /// <param name="provider">格式提供者。</param>
    public void add_format_provider(IVideoFormatProvider provider)
    {
        _format_providers.Add(provider);
    }

    /// <summary>
    ///     添加处理器。
    /// </summary>
    /// <param name="processor">处理器。</param>
    public void add_processor(IVideoProcessor processor)
    {
        _processors.Add(processor);
    }

    /// <summary>
    ///     添加视觉算子。
    /// </summary>
    /// <param name="visionOperator">视觉算子。</param>
    public void add_vision_operator(IVideoVisionOperator visionOperator)
    {
        _vision_operators.Add(visionOperator);
    }

    /// <summary>
    ///     添加导入器。
    /// </summary>
    /// <param name="importer">导入器。</param>
    public void add_importer(IVideoImporter importer)
    {
        _importers.Add(importer);
    }

    /// <summary>
    ///     添加导出器。
    /// </summary>
    /// <param name="exporter">导出器。</param>
    public void add_exporter(IVideoExporter exporter)
    {
        _exporters.Add(exporter);
    }
}
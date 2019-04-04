using Std.Video.Formats;

namespace Std.Video.Extensions;

/// <summary>
///     视频扩展宿主类，管理扩展的注册和组件的聚合。
/// </summary>
public sealed class VideoExtensionHost
{
    private readonly List<IVideoExporter> _exporters = [];
    private readonly List<IVideoExtension> _extensions = [];
    private readonly List<IVideoFormatHandler> _format_handlers = [];
    private readonly List<IVideoImporter> _importers = [];
    private readonly List<IVideoProcessor> _processors = [];
    private readonly List<IVideoVisionOperator> _vision_operators = [];

    /// <summary>
    ///     获取已注册的扩展。
    /// </summary>
    public IEnumerable<IVideoExtension> extensions => _extensions;

    /// <summary>
    ///     获取已注册的格式处理器。
    /// </summary>
    public IEnumerable<IVideoFormatHandler> format_handlers => _format_handlers;

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
    ///     添加视频扩展，并自动注册其所有组件。
    /// </summary>
    /// <param name="extension">视频扩展。</param>
    public void add(IVideoExtension extension)
    {
        _extensions.Add(extension);

        var context = new VideoExtensionContext();
        extension.register(context);

        foreach (var provider in context.format_providers)
        foreach (var handler in provider.create_handlers())
            _format_handlers.Add(handler);

        foreach (var processor in context.processors) _processors.Add(processor);

        foreach (var visionOperator in context.vision_operators) _vision_operators.Add(visionOperator);

        foreach (var importer in context.importers) _importers.Add(importer);

        foreach (var exporter in context.exporters) _exporters.Add(exporter);
    }

    /// <summary>
    ///     直接添加格式处理器。
    /// </summary>
    /// <param name="handler">格式处理器。</param>
    public void add_format_handler(IVideoFormatHandler handler)
    {
        _format_handlers.Add(handler);
    }

    /// <summary>
    ///     直接添加处理器。
    /// </summary>
    /// <param name="processor">处理器。</param>
    public void add_processor(IVideoProcessor processor)
    {
        _processors.Add(processor);
    }

    /// <summary>
    ///     直接添加视觉算子。
    /// </summary>
    /// <param name="visionOperator">视觉算子。</param>
    public void add_vision_operator(IVideoVisionOperator visionOperator)
    {
        _vision_operators.Add(visionOperator);
    }

    /// <summary>
    ///     直接添加导入器。
    /// </summary>
    /// <param name="importer">导入器。</param>
    public void add_importer(IVideoImporter importer)
    {
        _importers.Add(importer);
    }

    /// <summary>
    ///     直接添加导出器。
    /// </summary>
    /// <param name="exporter">导出器。</param>
    public void add_exporter(IVideoExporter exporter)
    {
        _exporters.Add(exporter);
    }
}
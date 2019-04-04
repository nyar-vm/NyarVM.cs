using Std.Image.Formats;

namespace Std.Image.Extensions;

/// <summary>
///     图像扩展宿主，管理扩展的注册和生命周期。
/// </summary>
public sealed class ImageExtensionHost
{
    private readonly List<IImageExporter> _exporters = [];
    private readonly List<IImageImporter> _importers = [];
    private readonly List<IImageProcessor> _processors = [];
    private readonly List<IImageVisionOperator> _visionOperators = [];

    /// <summary>
    ///     获取格式注册表。
    /// </summary>
    public ImageFormatRegistry registry { get; } = new();

    /// <summary>
    ///     获取已注册的处理器列表。
    /// </summary>
    public IReadOnlyList<IImageProcessor> processors => _processors;

    /// <summary>
    ///     获取已注册的视觉算子列表。
    /// </summary>
    public IReadOnlyList<IImageVisionOperator> vision_operators => _visionOperators;

    /// <summary>
    ///     获取已注册的导入器列表。
    /// </summary>
    public IReadOnlyList<IImageImporter> importers => _importers;

    /// <summary>
    ///     获取已注册的导出器列表。
    /// </summary>
    public IReadOnlyList<IImageExporter> exporters => _exporters;

    /// <summary>
    ///     注册扩展，将扩展中的所有组件注册到宿主。
    /// </summary>
    /// <param name="extension">图像扩展。</param>
    public void add(IImageExtension extension)
    {
        var context = new ImageExtensionContext();
        extension.register(context);

        foreach (var handler in context.handlers) registry.register_handler(handler);

        foreach (var detector in context.detectors) registry.register_detector(detector);

        foreach (var processor in context.processors) _processors.Add(processor);

        foreach (var visionOperator in context.vision_operators) _visionOperators.Add(visionOperator);

        foreach (var importer in context.importers) _importers.Add(importer);

        foreach (var exporter in context.exporters) _exporters.Add(exporter);
    }

    /// <summary>
    ///     直接添加格式处理器。
    /// </summary>
    /// <param name="handler">格式处理器。</param>
    public void add_format_handler(IImageFormatHandler handler)
    {
        registry.register_handler(handler);
    }

    /// <summary>
    ///     直接添加图像处理器。
    /// </summary>
    /// <param name="processor">图像处理器。</param>
    public void add_processor(IImageProcessor processor)
    {
        _processors.Add(processor);
    }

    /// <summary>
    ///     直接添加视觉算子。
    /// </summary>
    /// <param name="vision_operator">视觉算子。</param>
    public void add_vision_operator(IImageVisionOperator vision_operator)
    {
        _visionOperators.Add(vision_operator);
    }

    /// <summary>
    ///     直接添加图像导入器。
    /// </summary>
    /// <param name="importer">图像导入器。</param>
    public void add_importer(IImageImporter importer)
    {
        _importers.Add(importer);
    }

    /// <summary>
    ///     直接添加图像导出器。
    /// </summary>
    /// <param name="exporter">图像导出器。</param>
    public void add_exporter(IImageExporter exporter)
    {
        _exporters.Add(exporter);
    }
}
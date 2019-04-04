using Std.Image.Formats;

namespace Std.Image.Extensions;

/// <summary>
///     图像扩展注册上下文，提供各类扩展组件的注册方法。
/// </summary>
public sealed class ImageExtensionContext
{
    private readonly List<IImageFormatDetector> _detectors = [];
    private readonly List<IImageExporter> _exporters = [];
    private readonly List<IImageFormatHandler> _handlers = [];
    private readonly List<IImageImporter> _importers = [];
    private readonly List<IImageProcessor> _processors = [];
    private readonly List<IImageVisionOperator> _visionOperators = [];

    /// <summary>
    ///     获取已注册的格式处理器列表。
    /// </summary>
    public IReadOnlyList<IImageFormatHandler> handlers => _handlers;

    /// <summary>
    ///     获取已注册的格式探测器列表。
    /// </summary>
    public IReadOnlyList<IImageFormatDetector> detectors => _detectors;

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
    ///     添加格式处理器。
    /// </summary>
    /// <param name="handler">格式处理器。</param>
    public void add_handler(IImageFormatHandler handler)
    {
        _handlers.Add(handler);
    }

    /// <summary>
    ///     添加格式探测器。
    /// </summary>
    /// <param name="detector">格式探测器。</param>
    public void add_detector(IImageFormatDetector detector)
    {
        _detectors.Add(detector);
    }

    /// <summary>
    ///     添加图像处理器。
    /// </summary>
    /// <param name="processor">图像处理器。</param>
    public void add_processor(IImageProcessor processor)
    {
        _processors.Add(processor);
    }

    /// <summary>
    ///     添加视觉算子。
    /// </summary>
    /// <param name="vision_operator">视觉算子。</param>
    public void add_vision_operator(IImageVisionOperator vision_operator)
    {
        _visionOperators.Add(vision_operator);
    }

    /// <summary>
    ///     添加图像导入器。
    /// </summary>
    /// <param name="importer">图像导入器。</param>
    public void add_importer(IImageImporter importer)
    {
        _importers.Add(importer);
    }

    /// <summary>
    ///     添加图像导出器。
    /// </summary>
    /// <param name="exporter">图像导出器。</param>
    public void add_exporter(IImageExporter exporter)
    {
        _exporters.Add(exporter);
    }
}
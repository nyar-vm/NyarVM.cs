using Sonic.Audio.Formats;

namespace Sonic.Audio.Extensions;

/// <summary>
///     音频扩展宿主类，管理扩展的加载和组件注册到格式注册表。
/// </summary>
public sealed class AudioExtensionHost
{
    /// <summary>
    ///     音频分析器列表。
    /// </summary>
    private readonly List<IAudioAnalyzer> _analyzers = [];

    /// <summary>
    ///     音频导出器列表。
    /// </summary>
    private readonly List<IAudioExporter> _exporters = [];

    /// <summary>
    ///     扩展列表。
    /// </summary>
    private readonly List<IAudioExtension> _extensions = [];

    /// <summary>
    ///     格式提供者列表。
    /// </summary>
    private readonly List<IAudioFormatProvider> _formatProviders = [];

    /// <summary>
    ///     音频导入器列表。
    /// </summary>
    private readonly List<IAudioImporter> _importers = [];

    /// <summary>
    ///     音频处理器列表。
    /// </summary>
    private readonly List<IAudioProcessor> _processors = [];

    /// <summary>
    ///     添加扩展。
    /// </summary>
    /// <param name="extension">音频扩展。</param>
    public void add(IAudioExtension extension)
    {
        _extensions.Add(extension);
    }

    /// <summary>
    ///     添加格式提供者。
    /// </summary>
    /// <param name="provider">格式提供者。</param>
    public void add(IAudioFormatProvider provider)
    {
        _formatProviders.Add(provider);
    }

    /// <summary>
    ///     添加音频处理器。
    /// </summary>
    /// <param name="processor">音频处理器。</param>
    public void add(IAudioProcessor processor)
    {
        _processors.Add(processor);
    }

    /// <summary>
    ///     添加音频分析器。
    /// </summary>
    /// <param name="analyzer">音频分析器。</param>
    public void add(IAudioAnalyzer analyzer)
    {
        _analyzers.Add(analyzer);
    }

    /// <summary>
    ///     添加音频导入器。
    /// </summary>
    /// <param name="importer">音频导入器。</param>
    public void add(IAudioImporter importer)
    {
        _importers.Add(importer);
    }

    /// <summary>
    ///     添加音频导出器。
    /// </summary>
    /// <param name="exporter">音频导出器。</param>
    public void add(IAudioExporter exporter)
    {
        _exporters.Add(exporter);
    }

    /// <summary>
    ///     将所有已注册的扩展和格式提供者应用到格式注册表。
    /// </summary>
    /// <param name="registry">音频格式注册表。</param>
    public void apply_to(AudioFormatRegistry registry)
    {
        var context = new AudioExtensionContext();

        foreach (var extension in _extensions) extension.register(context);

        foreach (var handler in context.handlers) registry.register_handler(handler);

        foreach (var detector in context.detectors) registry.register_detector(detector);

        foreach (var provider in _formatProviders)
        {
            foreach (var detector in provider.create_detectors()) registry.register_detector(detector);

            foreach (var handler in provider.create_handlers()) registry.register_handler(handler);
        }
    }
}
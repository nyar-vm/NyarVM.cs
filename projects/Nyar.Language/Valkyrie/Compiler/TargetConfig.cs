namespace Nyar.Language.Valkyrie.Compiler;

/// <summary>
///     表示一个 VOA 编译目标平台的配置元数据。
/// </summary>
public sealed record TargetConfig
{
    /// <summary>
    ///     目标平台唯一标识符。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    ///     目标平台的显示名称。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    ///     是否包含 JavaScript 胶水代码。
    /// </summary>
    public bool HasJsGlue { get; init; }

    /// <summary>
    ///     是否支持渐进式 Web 应用（PWA）。
    /// </summary>
    public bool HasPwa { get; init; }

    /// <summary>
    ///     输出文件格式（如 wasm、dll、class、exe）。
    /// </summary>
    public string OutputFormat { get; init; } = string.Empty;

    /// <summary>
    ///     目标 CPU 架构。
    /// </summary>
    public string Arch { get; init; } = string.Empty;

    /// <summary>
    ///     目标平台 ABI。
    /// </summary>
    public string Abi { get; init; } = string.Empty;
}
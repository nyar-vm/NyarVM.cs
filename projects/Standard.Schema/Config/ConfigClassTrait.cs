namespace Hermes.Config;

/// <summary>
///     配置类特征 — 从 ClassDefinition 提取的 config 元数据
/// </summary>
public sealed class ConfigClassTrait
{
    public ConfigClassTrait(
        string className,
        string @namespace,
        IReadOnlyList<string> sources,
        IReadOnlyList<ConfigFieldTrait> fields,
        bool hasFallback,
        string? fallbackSource)
    {
        ClassName = className;
        Namespace = @namespace;
        Sources = sources;
        Fields = fields;
        HasFallback = hasFallback;
        FallbackSource = fallbackSource;
    }

    /// <summary>
    ///     类名
    /// </summary>
    public string ClassName { get; }

    /// <summary>
    ///     所属命名空间
    /// </summary>
    public string Namespace { get; }

    /// <summary>
    ///     Config source 声明（从左到右优先级递增）
    /// </summary>
    public IReadOnlyList<string> Sources { get; }

    /// <summary>
    ///     字段特征列表
    /// </summary>
    public IReadOnlyList<ConfigFieldTrait> Fields { get; }

    /// <summary>
    ///     是否有降级 source
    /// </summary>
    public bool HasFallback { get; }

    /// <summary>
    ///     降级 source（Provider 不可达时使用）
    /// </summary>
    public string? FallbackSource { get; }
}
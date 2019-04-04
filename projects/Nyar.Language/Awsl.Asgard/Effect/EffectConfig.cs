namespace Valkyrie.Asgard.Effect;

/// <summary>
///     Effect 副作用配置
/// </summary>
public sealed class EffectConfig
{
    /// <summary>重试次数</summary>
    public int retry_count { get; set; } = 3;

    /// <summary>重试间隔延迟（毫秒）</summary>
    public int retry_delay_ms { get; set; } = 1000;

    /// <summary>超时时间（毫秒）</summary>
    public int timeout_ms { get; set; } = 30000;

    /// <summary>缓存 TTL（毫秒），0 表示不缓存</summary>
    public int cache_ttl_ms { get; set; }

    /// <summary>是否去重</summary>
    public bool dedupe { get; set; } = true;

    /// <summary>默认配置</summary>
    public static EffectConfig @default => new();
}
